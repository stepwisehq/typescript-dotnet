#!/usr/bin/env python3
"""Authored paired public stdio control; no compiler execution during preparation.

Run pinned Go and candidate commands sequentially against the same real temporary
workspace. Preserve every raw frame/stderr byte and compare complete decoded response
payloads. Cancellation and an edit racing a retained request have exactly the source
admissible outcomes described below; no blanket diagnostic normalization.
"""
import argparse, base64, hashlib, json, os, queue, subprocess, threading, time
from pathlib import Path


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def decode(data):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError('duplicate response field: ' + key)
            result[key] = value
        return result
    return json.loads(data.decode('utf-8'), object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError(value)))


# A genuine NoParams message omits the member; explicit JSON null stays distinct.
NO_PARAMS = object()


class Client:
    def __init__(self, command, directory, artifact):
        artifact.mkdir(parents=True, exist_ok=False)
        self.artifact, self.counter = artifact, 0
        self.incoming, self.pending = queue.Queue(), {}
        self.lock = threading.Lock()
        self.transcript = (artifact / 'frames.jsonl').open('w')
        self.stderr = (artifact / 'stderr.txt').open('wb')
        self.started = time.monotonic()
        self.process = subprocess.Popen(command, cwd=directory, stdin=subprocess.PIPE,
                                        stdout=subprocess.PIPE, stderr=self.stderr)
        self.command = command
        self.reader = threading.Thread(target=self.read, daemon=True)
        self.reader.start()
        self.memory = []
        self.operation_timings = {}

    def record(self, direction, payload):
        with self.lock:
            self.transcript.write(json.dumps({'direction': direction,
                'elapsedSeconds': time.monotonic() - self.started,
                'sha256': hashlib.sha256(payload).hexdigest(),
                'rawBase64': base64.b64encode(payload).decode(), 'message': decode(payload)}) + '\n')
            self.transcript.flush()

    def read_exact(self, count):
        result = bytearray()
        while len(result) < count:
            part = self.process.stdout.read(count - len(result))
            if not part:
                raise EOFError('child closed stdout during a frame')
            result.extend(part)
        return bytes(result)

    def read(self):
        try:
            while True:
                headers = {}
                while True:
                    line = self.process.stdout.readline()
                    if not line:
                        raise EOFError('child closed stdout')
                    if line in (b'\r\n', b'\n'):
                        break
                    key, value = line.split(b':', 1)
                    key = key.strip().lower()
                    if key in headers:
                        raise ValueError('duplicate transport header')
                    headers[key] = value.strip()
                count = int(headers[b'content-length'])
                if count < 0:
                    raise ValueError('negative Content-Length')
                payload = self.read_exact(count)
                self.record('received', payload)
                message = decode(payload)
                if not isinstance(message, dict):
                    raise ValueError('response root must be object')
                self.incoming.put(message)
        except Exception as error:
            self.incoming.put(error)

    def send(self, message):
        payload = json.dumps(message, ensure_ascii=False, separators=(',', ':'), allow_nan=False).encode()
        self.record('sent', payload)
        self.process.stdin.write(b'Content-Length: ' + str(len(payload)).encode() + b'\r\n\r\n' + payload)
        self.process.stdin.flush()

    def notify(self, method, parameters=NO_PARAMS):
        message = {'jsonrpc': '2.0', 'method': method}
        if parameters is not NO_PARAMS:
            message['params'] = parameters
        self.send(message)

    def start(self, method, parameters=NO_PARAMS):
        self.counter += 1
        message = {'jsonrpc': '2.0', 'id': self.counter, 'method': method}
        if parameters is not NO_PARAMS:
            message['params'] = parameters
        self.send(message)
        return self.counter

    def wait(self, identifier):
        deadline = time.monotonic() + 30  # Test deadlock safety, not a latency assertion.
        while identifier not in self.pending:
            message = self.incoming.get(timeout=max(0, deadline - time.monotonic()))
            if isinstance(message, Exception):
                raise message
            if 'method' in message:
                if 'id' in message:
                    method = message['method']
                    if method == 'workspace/configuration':
                        result = [{} for _ in message['params']['items']]
                    elif method in ('client/registerCapability', 'client/unregisterCapability',
                                    'window/workDoneProgress/create', 'workspace/diagnostic/refresh',
                                    'workspace/inlayHint/refresh', 'workspace/codeLens/refresh'):
                        result = None
                    else:
                        self.send({'jsonrpc': '2.0', 'id': message['id'], 'error':
                                   {'code': -32601, 'message': 'Unknown method: ' + method}})
                        continue
                    self.send({'jsonrpc': '2.0', 'id': message['id'], 'result': result})
                continue  # Notifications remain fully retained in the transcript.
            if 'id' not in message or message['id'] in self.pending:
                raise ValueError('unmatched/duplicate response ID')
            self.pending[message['id']] = message
        response = self.pending.pop(identifier)
        if response.get('jsonrpc') != '2.0':
            raise ValueError('incorrect response version')
        return response

    def request(self, method, parameters=NO_PARAMS):
        response = self.wait(self.start(method, parameters))
        if 'error' in response:
            raise AssertionError(response['error'])
        if 'result' not in response:
            raise ValueError('response has no result')
        return response['result']

    def sample_memory(self, phase):
        values = {}
        status = Path('/proc') / str(self.process.pid) / 'status'
        if status.exists():
            for line in status.read_text().splitlines():
                if line.startswith(('VmRSS:', 'VmHWM:', 'Threads:')):
                    key, value = line.split(':', 1)
                    values[key] = value.strip()
        self.memory.append({'phase': phase, 'linuxProcStatus': values})

    def close(self, exit_notification=False):
        # EOF and exit-notification are distinct upstream termination paths.
        # Keep stdin open for exit: closing it would introduce a competing EOF.
        if not exit_notification:
            self.process.stdin.close()
        try:
            code = self.process.wait(timeout=30)
        except subprocess.TimeoutExpired:
            self.process.kill()
            self.process.wait()
            raise
        finally:
            if exit_notification:
                self.process.stdin.close()
            self.reader.join(timeout=1)
            self.stderr.close()
            self.transcript.close()
            (self.artifact / 'process.json').write_text(json.dumps({
                'command': self.command, 'binarySha256': sha(self.command[0]),
                'exitCode': self.process.returncode, 'wallSeconds': time.monotonic() - self.started,
                'terminationRole': 'exit-notification' if exit_notification else 'input-eof',
                'memory': self.memory, 'operationTimingsSeconds': self.operation_timings}, indent=2) + '\n')
        stderr = (self.artifact / 'stderr.txt').read_bytes()
        expected = (1, b'context canceled\n') if exit_notification else (0, b'')
        if (code, stderr) != expected:
            raise AssertionError('public LSP termination ' + repr((code, stderr)) + ' != ' + repr(expected))
        return {'exitCode': code, 'stderrBase64': base64.b64encode(stderr).decode(),
                'role': 'exit-notification' if exit_notification else 'input-eof'}


def diagnostic_kinds(report, expected, phase):
    if not isinstance(report, dict) or report.get('kind') != 'full' or not isinstance(report.get('items'), list):
        raise AssertionError(phase + ': expected complete full diagnostic report')
    actual = [(item.get('code'), item.get('severity')) for item in report['items']]
    if actual != expected:
        raise AssertionError(phase + ': diagnostic (code, severity) ' + repr(actual) + ' != ' + repr(expected))


def scenario(command, workspace, artifact, repeats):
    dependency = workspace / 'dependency.ts'
    dependency.unlink(missing_ok=True)  # Each child starts with identical initial workspace contents.
    configuration = workspace / 'tsconfig.json'
    configuration.write_text('{"compilerOptions":{"strict":true,"skipLibCheck":true}}\n')
    (workspace / 'index.ts').write_text('export const value: number = 1;\n')
    uri, config_uri = (workspace / 'index.ts').as_uri(), configuration.as_uri()
    document = {'textDocument': {'uri': uri}}
    client, results = Client(command, workspace, artifact), {}
    sent_exit = False
    try:
        results['initialize'] = client.request('initialize', {
            'processId': None, 'rootUri': workspace.as_uri(),
            'capabilities': {'general': {'positionEncodings': ['utf-8']},
                'workspace': {'configuration': True}, 'textDocument': {'diagnostic': {}, 'semanticTokens': {
                    'requests': {'full': True}, 'formats': ['relative'],
                    'tokenTypes': ['namespace', 'class', 'enum', 'interface', 'struct', 'typeParameter',
                        'type', 'parameter', 'variable', 'property', 'enumMember', 'decorator', 'event',
                        'function', 'method', 'macro', 'label', 'comment', 'string', 'keyword',
                        'number', 'regexp', 'operator'],
                    'tokenModifiers': ['declaration', 'definition', 'readonly', 'static', 'deprecated',
                        'abstract', 'async', 'modification', 'documentation', 'defaultLibrary', 'local']}},
                'window': {'workDoneProgress': False}}})
        client.operation_timings['cold-process-through-initialize'] = [time.monotonic() - client.started]
        client.notify('initialized', {})
        client.notify('textDocument/didOpen', {'textDocument': {'uri': uri,
            'languageId': 'typescript', 'version': 1, 'text': 'export const value: number = 1;\n'}})
        results['valid'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['valid'], [], 'assembled valid environment')
        client.sample_memory('initialized-valid')
        client.notify('textDocument/didChange', {'textDocument': {'uri': uri, 'version': 2},
            'contentChanges': [{'text': 'export const value: string = 1;\n'}]})
        results['unsaved-invalid'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['unsaved-invalid'], [(2322, 1)], 'unsaved type mismatch')
        stale = client.start('textDocument/diagnostic', document)
        client.notify('textDocument/didChange', {'textDocument': {'uri': uri, 'version': 3},
            'contentChanges': [{'text': 'export function f(x) { return x; }\n'}]})
        stale_response = client.wait(stale)
        results['latest-before-config'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['latest-before-config'], [(7006, 1)], 'strict implicit-any parameter')
        if 'error' in stale_response or stale_response.get('result') not in (
                results['unsaved-invalid'], results['latest-before-config']):
            raise AssertionError('racing retained diagnostic was neither complete old nor new snapshot')
        # Its complete response remains in the transcript; scheduling is source-admissible.
        results['staleOutcome'] = 'old' if stale_response['result'] == results['unsaved-invalid'] else 'new'
        configuration.write_text('{"compilerOptions":{"strict":false,"skipLibCheck":true}}\n')
        client.notify('workspace/didChangeWatchedFiles', {'changes': [{'uri': config_uri, 'type': 2}]})
        client.notify('workspace/didChangeConfiguration', {'settings': {}})
        # ls.getAllDiagnostics includes suggestions; checker.reportImplicitAny selects
        # 7044 (LSP Hint) when noImplicitAny is false, rather than dropping it.
        results['after-config'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['after-config'], [(7044, 4)], 'disk config disables strict: implicit-any becomes suggestion')
        dependency = workspace / 'dependency.ts'
        dependency.write_text('export const imported = 1;\n')
        client.notify('workspace/didChangeWatchedFiles', {'changes': [{'uri': dependency.as_uri(), 'type': 1}]})
        client.notify('textDocument/didChange', {'textDocument': {'uri': uri, 'version': 4},
            'contentChanges': [{'text': 'import { imported } from "./dependency"; export const value: number = imported;\n'}]})
        results['dependency-valid'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['dependency-valid'], [], 'real dependency initial number export')
        dependency.write_text('export const imported = "changed";\n')
        client.notify('workspace/didChangeWatchedFiles', {'changes': [{'uri': dependency.as_uri(), 'type': 2}]})
        results['dependency-changed'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['dependency-changed'], [(2322, 1)], 'real dependency changed to string export')
        dependency.write_text('export const imported = 2;\n')
        client.notify('workspace/didChangeWatchedFiles', {'changes': [{'uri': dependency.as_uri(), 'type': 2}]})
        results['dependency-restored'] = client.request('textDocument/diagnostic', document)
        diagnostic_kinds(results['dependency-restored'], [], 'real dependency restored number export')
        baseline_tokens = client.request('textDocument/semanticTokens/full', document)
        if not isinstance(baseline_tokens, dict) or not isinstance(baseline_tokens.get('data'), list) or \
                len(baseline_tokens['data']) < 5 or len(baseline_tokens['data']) % 5:
            raise AssertionError('expected nonempty complete semantic token tuples')
        results['tokens'] = baseline_tokens
        canceled = client.start('textDocument/semanticTokens/full', document)
        client.notify('$/cancelRequest', {'id': canceled})
        response = client.wait(canceled)
        if 'error' in response:
            if response['error'] != {'code': -32800, 'message': 'RequestCancelled'}:
                raise AssertionError(response['error'])
            results['cancelOutcome'] = 'canceled'
        elif response.get('result') == baseline_tokens:
            results['cancelOutcome'] = 'completed-before-cancel'
        else:
            raise AssertionError('cancellation success differed from complete uncanceled result')
        query_timings = client.operation_timings['unchanged-diagnostic-queries'] = []
        for index in range(repeats):
            began = time.monotonic()
            report = client.request('textDocument/diagnostic', document)
            query_timings.append(time.monotonic() - began)
            if report != results['dependency-restored']:
                raise AssertionError('repeated diagnostic changed')
            if index in (0, repeats // 2, repeats - 1):
                client.sample_memory('repeat-' + str(index + 1))
        # Actual repeated unsaved edits, not merely stable queries. The restored
        # dependency exports number, so alternating assignment annotations alternate
        # valid and Error2322 reports. Each complete ordered report is compared.
        edit_reports = results['repeated-edit-diagnostics'] = []
        edit_timings = client.operation_timings['edit-and-diagnostic'] = []
        for index in range(repeats):
            invalid = index % 2 == 1
            annotation = 'string' if invalid else 'number'
            began = time.monotonic()
            client.notify('textDocument/didChange', {'textDocument': {'uri': uri, 'version': 5 + index},
                'contentChanges': [{'text': 'import { imported } from "./dependency"; export const value: ' +
                    annotation + ' = imported;\n'}]})
            report = client.request('textDocument/diagnostic', document)
            edit_timings.append(time.monotonic() - began)
            diagnostic_kinds(report, [(2322, 1)] if invalid else [], 'repeated unsaved edit ' + str(index))
            edit_reports.append(report)
            if index in (0, repeats // 2, repeats - 1):
                client.sample_memory('edit-and-diagnostic-' + str(index))
        client.notify('textDocument/didClose', document)
        results['shutdown'] = client.request('shutdown')
        if results['shutdown'] is not None:
            raise AssertionError('expected complete shutdown null result')
        client.notify('exit')
        sent_exit = True
        return results
    finally:
        termination = client.close(exit_notification=sent_exit)
        if sent_exit:
            results['termination'] = termination
            (artifact / 'results.json').write_text(json.dumps(results, indent=2) + '\n')


def eof_scenario(command, workspace, artifact):
    # No exit notification: after a successful shutdown response, input EOF is the
    # group's originating EOF and the public command returns zero without stderr.
    (workspace / 'dependency.ts').unlink(missing_ok=True)
    (workspace / 'tsconfig.json').write_text('{"compilerOptions":{"strict":true,"skipLibCheck":true}}\n')
    (workspace / 'index.ts').write_text('export const value: number = 1;\n')
    client, results = Client(command, workspace, artifact), {}
    try:
        results['initialize'] = client.request('initialize', {
            'processId': None, 'rootUri': workspace.as_uri(),
            'capabilities': {'general': {'positionEncodings': ['utf-8']},
                             'workspace': {'configuration': True}}})
        client.notify('initialized', {})
        results['shutdown'] = client.request('shutdown')
        if results['shutdown'] is not None:
            raise AssertionError('expected complete shutdown null result before EOF')
    finally:
        results['termination'] = client.close()
        (artifact / 'results.json').write_text(json.dumps(results, indent=2) + '\n')
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--go-command', required=True, help='JSON array: pinned Go public CLI + --lsp --stdio')
    parser.add_argument('--native-command', required=True, help='JSON array: actual Native public CLI + --lsp --stdio')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--repeats', type=int, default=100)
    args = parser.parse_args()
    if args.repeats < 1:
        parser.error('--repeats must be positive')
    args.output = args.output.resolve()
    args.output.mkdir(parents=True, exist_ok=False)
    workspace = args.output / 'workspace'
    workspace.mkdir()
    go_command, native_command = json.loads(args.go_command), json.loads(args.native_command)
    for command in (go_command, native_command):
        if not isinstance(command, list) or not command or not all(isinstance(part, str) for part in command):
            parser.error('commands must be nonempty JSON string arrays')
        command[0] = str(Path(command[0]).resolve(strict=True))
    go = scenario(go_command, workspace, args.output / 'go', args.repeats)
    native = scenario(native_command, workspace, args.output / 'native', args.repeats)
    go['shutdown-input-eof'] = eof_scenario(go_command, workspace, args.output / 'go-eof')
    native['shutdown-input-eof'] = eof_scenario(native_command, workspace, args.output / 'native-eof')
    exact_fields = [key for key in go if key not in ('cancelOutcome', 'staleOutcome')]
    differing = [key for key in exact_fields if go[key] != native[key]]
    report = {'status': 'pass' if not differing else 'fail', 'differingCompletePayloads': differing,
        'goRaceOutcomes': {key: go[key] for key in ('cancelOutcome', 'staleOutcome')},
        'nativeRaceOutcomes': {key: native[key] for key in ('cancelOutcome', 'staleOutcome')},
        'repeatedUnchangedDiagnosticsEach': args.repeats, 'repeatedUnsavedEditsEach': args.repeats, 'toolSha256': sha(__file__),
        'scope': 'Authored real-disk public stdio differential; no original upstream pass credit, universal memory threshold or performance claim. Full payloads and race outputs retained.'}
    (args.output / 'comparison.json').write_text(json.dumps(report, indent=2) + '\n')
    if differing:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
