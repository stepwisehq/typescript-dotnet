#!/usr/bin/env python3
"""Authored paired Linux public signal/declared-parent lifecycle observations.
No production changes, substitute server, arbitrary nonzero acceptance or time SLA.
"""
import argparse
import base64
import importlib.util
import json
import os
import signal
import subprocess
import sys
import time
from pathlib import Path

SOURCE = Path(__file__).with_name('compare-public-lsp.py')
spec = importlib.util.spec_from_file_location('public_stdio_driver', SOURCE)
shared = importlib.util.module_from_spec(spec)
spec.loader.exec_module(shared)
ROLES = ('sigint', 'sigterm', 'initialize-parent', 'flag-parent')


def finish(client, role, parent_pid):
    """Wait with input open, retaining exact role-specific cancellation stderr."""
    forced = False
    try:
        client.process.wait(timeout=30)  # Deadlock safety; source watchdog ticks at5s.
    except subprocess.TimeoutExpired:
        forced = True
        client.process.kill()
        client.process.wait()
        raise
    finally:
        client.process.stdin.close()
        client.reader.join(timeout=30)
        client.stderr.close()
        client.transcript.close()
        stderr = (client.artifact / 'stderr.txt').read_bytes()
        outcome = {'role': role, 'declaredParentPid': parent_pid,
                   'exitCode': client.process.returncode,
                   'stderrBase64': base64.b64encode(stderr).decode(),
                   'forcedKill': forced,
                   'wallSeconds': time.monotonic() - client.started,
                   'command': client.command, 'binarySha256': shared.sha(client.command[0])}
        (client.artifact / 'process.json').write_text(json.dumps(outcome, indent=2) + '\n')
    expected = b'context canceled\n'
    if parent_pid is not None:
        expected = ('Parent process ' + str(parent_pid) + ' has exited, shutting down.\n').encode() + expected
    if client.process.returncode != 1 or stderr != expected or forced:
        raise AssertionError('unexpected ' + role + ' terminal outcome: ' + repr(outcome))
    return outcome


def scenario(command, workspace, artifact, role):
    (workspace / 'tsconfig.json').write_text('{"compilerOptions":{"strict":true,"skipLibCheck":true}}\n')
    text = 'export const value: number = 1;\n'
    (workspace / 'index.ts').write_text(text)
    # The public protocol and CLI accept a declared editor PID independently of
    # OS PPID. This owned isolated process is that actual liveness target; it is
    # reaped before observation, so a zombie cannot make kill(pid,0) stay alive.
    parent = None
    client = None
    triggered = False
    try:
        if role.endswith('-parent'):
            parent = subprocess.Popen([sys.executable, '-c', 'import sys; sys.stdin.buffer.read()'],
                                      stdin=subprocess.PIPE, stdout=subprocess.DEVNULL,
                                      stderr=subprocess.DEVNULL)
        actual_command = command + (['--clientProcessId=' + str(parent.pid)] if role == 'flag-parent' else [])
        client = shared.Client(actual_command, workspace, artifact)
        params = {'processId': parent.pid if role == 'initialize-parent' else None,
                  'rootUri': workspace.as_uri(),
                  'capabilities': {'general': {'positionEncodings': ['utf-8']},
                                   'workspace': {'configuration': True},
                                   'textDocument': {'diagnostic': {}}}}
        results = {'initialize': client.request('initialize', params)}
        client.notify('initialized', {})
        uri = (workspace / 'index.ts').as_uri()
        client.notify('textDocument/didOpen', {'textDocument': {
            'uri': uri, 'languageId': 'typescript', 'version': 1, 'text': text}})
        results['valid'] = client.request('textDocument/diagnostic', {'textDocument': {'uri': uri}})
        shared.diagnostic_kinds(results['valid'], [], role + ': responsive before termination')
        if parent is None:
            client.process.send_signal(signal.SIGINT if role == 'sigint' else signal.SIGTERM)
        else:
            parent.terminate()
            parent.wait(timeout=30)
        triggered = True
        terminal = finish(client, role, parent.pid if parent is not None else None)
        (artifact / 'results.json').write_text(json.dumps(results, indent=2) + '\n')
        return results, terminal
    finally:
        if client is not None and not triggered:
            # Failure cleanup only, never substituted for a validated lifecycle.
            if client.process.poll() is None:
                client.process.kill()
            client.process.wait()
            client.process.stdin.close()
            client.reader.join(timeout=30)
            client.stderr.close()
            client.transcript.close()
        if parent is not None:
            if parent.poll() is None:
                parent.kill()
            parent.wait()
            parent.stdin.close()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--go-command', required=True)
    parser.add_argument('--native-command', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    if not sys.platform.startswith('linux'):
        parser.error('This authored lifecycle workload requires Linux signal/process semantics')
    commands = {}
    for name, raw in (('go', args.go_command), ('native', args.native_command)):
        command = json.loads(raw)
        if not isinstance(command, list) or not command or not all(isinstance(x, str) for x in command):
            parser.error('commands must be nonempty JSON string arrays')
        command[0] = str(Path(command[0]).resolve(strict=True))
        commands[name] = command
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    workspace = output / 'workspace'
    workspace.mkdir()
    observations = {}
    differences = []
    for role in ROLES:
        observations[role] = {}
        for name, command in commands.items():
            payloads, terminal = scenario(command, workspace, output / (name + '-' + role), role)
            observations[role][name] = {'payloads': payloads, 'termination': terminal}
        if observations[role]['go']['payloads'] != observations[role]['native']['payloads']:
            differences.append(role)
    result = {'status': 'fail' if differences else 'pass', 'differentCompletePayloadRoles': differences,
              'observations': observations, 'toolSha256': shared.sha(__file__),
              'sharedClientSha256': shared.sha(SOURCE),
              'scope': 'Authored public Linux signal and declared-parent/override watchdog evidence; zero original credit. Exact PID-specific terminal stderr verified separately and retained, complete response payloads paired. No latency/resource parity claim.'}
    (output / 'comparison.json').write_text(json.dumps(result, indent=2) + '\n')
    if differences:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
