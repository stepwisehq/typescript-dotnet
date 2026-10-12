#!/usr/bin/env python3
"""Compare original public outputs and every recorded original assertion channel."""
import argparse
import base64
import gzip
import json
from pathlib import Path


def rows(path):
    opener = gzip.open if str(path).endswith('.gz') else open
    with opener(path, 'rt', encoding='utf-8') as stream:
        for line in stream:
            yield json.loads(line)


def first_difference(expected, actual, path=''):
    if type(expected) is not type(actual):
        return path + ': value type differs'
    if isinstance(expected, dict):
        if expected.keys() != actual.keys():
            return path + ': object fields differ'
        for key in expected:
            difference = first_difference(expected[key], actual[key], path + '/' + key)
            if difference:
                return difference
    elif isinstance(expected, list):
        if len(expected) != len(actual):
            return path + f': array length {len(expected)} != {len(actual)}'
        for i, (left, right) in enumerate(zip(expected, actual)):
            difference = first_difference(left, right, path + '/' + str(i))
            if difference:
                return difference
    elif expected != actual:
        return path + ': value differs'
    return None


BASELINE_FIELDS = {
    'diagnostics': ['errorBaselineBase64'],
    'contentMapper': ['contentMapperBaselineBase64'],
    'output': ['javaScriptBaselineBase64'],
    'sourceMap': ['sourceMapBaselineBase64'],
    'sourceMapRecord': ['sourceMapRecordBaselineBase64'],
    'typesAndSymbols': ['typesBase64', 'symbolsBase64'],
    'moduleResolution': ['moduleResolutionBaselineBase64'],
    'unionOrdering': [], 'parentPointers': [],
}


def original_channel_differences(original, managed, supplements):
    differences = []
    for channel, expected in original['channels'].items():
        actual = managed.get('channels', {}).get(channel)
        if actual is None or actual.get('status') != expected['status']:
            differences.append({'channel': channel, 'difference': 'original channel disposition differs', 'actual': actual})
            continue
        if expected['status'] != 'passed-original-assertions':
            continue
        records = expected['baselines']
        if channel == 'sourceMapRecord' and not records:
            supplemental = supplements.get(original['name'])
            if supplemental is None or any(supplemental[field] != original[field] for field in ['sourceSha256', 'configuration']):
                differences.append({'channel': channel, 'difference': 'exact supplemental original record identity missing'})
                continue
            records = supplemental['channels'][channel]['baselines']
        observed_fields = set()
        for record in records:
            if channel == 'typesAndSymbols':
                if record['path'].endswith('.types'):
                    field = 'typesBase64'
                elif record['path'].endswith('.symbols'):
                    field = 'symbolsBase64'
                else:
                    raise ValueError('Unexpected original type/symbol baseline path: ' + record['path'])
            else:
                if len(BASELINE_FIELDS[channel]) != 1:
                    raise ValueError('Unexpected recorded baseline in channel ' + channel)
                field = BASELINE_FIELDS[channel][0]
            observed_fields.add(field)
            if record['contentBase64'] != managed.get(field):
                differences.append({'channel': channel, 'baseline': record['path'], 'field': field,
                                    'difference': 'complete original formatted baseline bytes differ'})
        for field in set(BASELINE_FIELDS[channel]) - observed_fields:
            if managed.get(field) is not None:
                differences.append({'channel': channel, 'field': field, 'difference': 'managed wrote a baseline absent in original channel'})
    if managed.get('pendingOriginalChannels') != []:
        differences.append({'difference': 'original assertion implementation remains pending', 'pending': managed.get('pendingOriginalChannels')})
    for invariant in ['parentPointersBeforeEmit', 'parentPointersAfterEmit', 'unionOrderingBeforeEmit', 'unionOrderingAfterEmit']:
        if managed.get('originalAssertions', {}).get(invariant) is not True:
            differences.append({'difference': 'original AST invariant missing/failed', 'invariant': invariant})
    return differences


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('original', type=Path)
    parser.add_argument('managed', type=Path)
    parser.add_argument('result', type=Path)
    parser.add_argument('--requests', type=Path, help='Restrict expected identity to this original request batch.')
    parser.add_argument('--supplement', type=Path, default=Path(__file__).resolve().parents[2] / 'artifacts/emit/whole-reference-record-supplement/original.jsonl', help='Exact eleven-record sourceMapRecord collision supplement.')
    args = parser.parse_args()
    supplements = {row['name']: row for row in rows(args.supplement)}
    requested = None
    if args.requests:
        requested = {row['Name'] for row in rows(args.requests)}
    originals = {}
    for row in rows(args.original):
        if requested is not None and row['name'] not in requested:
            continue
        if row['name'] in originals:
            raise ValueError('Duplicate original identity: ' + row['name'])
        originals[row['name']] = row
    seen = set()
    counts = {'matchedPublicOutputs': 0, 'failed': 0, 'originalPreparationSkips': 0, 'originalOptionPolicies': 0, 'matchedOriginalAssertions': 0, 'externalIncrementalOwnerBlocked': 0, 'originalIncrementalWrapperRequired': 0}
    failures = []
    comparisons = ['diagnostics', 'emitResult', 'owners', 'js', 'dts', 'maps']
    for row in rows(args.managed):
        name = row['name']
        if name in seen or name not in originals:
            raise ValueError('Duplicate or unexpected managed identity: ' + name)
        seen.add(name)
        original = originals[name]
        if row['status'] != 'failed-managed-runtime' and (row.get('sourceSha256') != original['sourceSha256'] or row.get('configuration') != original['configuration']):
            raise ValueError('Configuration/source identity changed: ' + name)
        if original['status'] == 'skipped-original-before-configuration':
            if row['status'] != original['status']:
                failures.append({'name': name, 'difference': 'original preparation skip differs'})
                counts['failed'] += 1
            else:
                counts['originalPreparationSkips'] += 1
            continue
        if original['status'] == 'original-option-policy-disposition':
            counts['originalOptionPolicies'] += 1
        if row.get('originalIncrementalWrapperRequired'):
            counts['originalIncrementalWrapperRequired'] += 1
        if row['status'] != 'observed-managed-emit':
            failures.append({'name': name, 'difference': 'managed runtime failure', 'observation': row})
            counts['failed'] += 1
            continue
        difference = None
        for field in comparisons:
            difference = first_difference(original[field], row[field], '/' + field)
            if difference:
                break
        channels = original_channel_differences(original, row, supplements) if original['status'] == 'passed-original-full-harness' else []
        expected_policy = {'passed-original-assertions': 'run', 'skipped-original-channel': 'skip', 'failed-original-assertions': 'fatal'}[original['optionPolicy']['status']]
        if row.get('originalOptionPolicy', {}).get('decision') != expected_policy:
            channels.append({'channel': 'original-option-policy', 'difference': 'original option-policy disposition differs', 'expected': expected_policy, 'actual': row.get('originalOptionPolicy')})
        if not difference:
            counts['matchedPublicOutputs'] += 1
        if original['status'] == 'passed-original-full-harness' and not channels:
            counts['matchedOriginalAssertions'] += 1
        if difference or channels:
            counts['failed'] += 1
            failure = {'name': name, 'difference': difference, 'originalChannelDifferences': channels}
            # Do not classify unrelated diagnostics/output/channel failures as external.
            expected_emit = dict(original['emitResult'])
            actual_emit = dict(row['emitResult'])
            expected_paths = expected_emit.pop('emittedFiles')
            actual_paths = actual_emit.pop('emittedFiles')
            non_buildinfo = [path for path in expected_paths or []
                             if not base64.b64decode(path['bytesBase64']).endswith(b'.tsbuildinfo')]
            only_buildinfo_difference = (
                row.get('originalIncrementalWrapperRequired') and not channels
                and expected_emit == actual_emit and (actual_paths or []) == non_buildinfo
                and expected_paths != actual_paths
                and all(original[field] == row[field] for field in comparisons if field != 'emitResult')
            )
            if only_buildinfo_difference:
                counts['externalIncrementalOwnerBlocked'] += 1
                failure['boundary'] = 'only original execute/incremental.NewProgram-owned tsbuildinfo emitted-file metadata differs; mismatch remains visible'
            failures.append(failure)
    missing = sorted(originals.keys() - seen)
    result = {
        'classification': 'original-public-program-output-and-complete-original-channel-comparison',
        'comparedFields': comparisons, 'expected': len(originals), 'observed': len(seen),
        **counts, 'missing': missing, 'failures': failures,
        'pendingOriginalAssertions': [],
        'originalChannels': list(BASELINE_FIELDS),
        'externalBoundaryDoesNotEraseFailures': True,
        'ordering': 'original input-ordered EmitResult and original per-input output grouping; no blanket sorting or normalization',
    }
    args.result.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: value for key, value in result.items() if key not in ('failures', 'pendingOriginalAssertions', 'ordering')}))
    return 1 if counts['failed'] or missing else 0


if __name__ == '__main__':
    raise SystemExit(main())
