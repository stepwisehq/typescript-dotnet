// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Port of tsc/internal/compiler/checkerpool.go at fed0bf24149fb1ed36039212648bafdafc1ea10e.
using System.Runtime.CompilerServices;
using TypeScript.Scanner;
using TypeScript.Checking;
using TypeScript.Modules;

[assembly: InternalsVisibleTo("Program.Checks")]
[assembly: InternalsVisibleTo("Program.Validation")]
[assembly: InternalsVisibleTo("Declarations.Checks")]
[assembly: InternalsVisibleTo("Declarations.Validation")]
namespace TypeScript.Compiler;
public interface ICheckerPool
{
    (Checker Checker, Action Done) GetChecker(CancellationToken context, SourceFile? file);
}

internal readonly record struct CheckerAssociationPolicy(bool prioritizeSourceFiles, long sourceFileWeightMultiplier, long balancePenaltyMultiplier);
internal sealed class CheckerPool : ICheckerPool
{
    internal const long checkerAssociationTextWeightDivisor = 100;
    internal const long checkerAssociationSourceFileWeightMultiplier = 4;
    internal const long checkerAssociationBalancePenaltyMultiplier = 16;
    internal const long checkerAssociationPrioritizedSourcePenalty = 12;
    internal const long checkerAssociationStrongBalanceMinCheckerCount = 4;
    internal readonly Program program;
    internal readonly TypeScript.Tracing.Tracing? tracing;
    internal readonly Checker[] checkers;
    internal readonly SemaphoreSlim[] locks;
    internal Dictionary<SourceFile, Checker> fileAssociations = new(ReferenceEqualityComparer.Instance);
    private readonly Lazy<bool> createCheckersOnce;
    internal CheckerPool(Program program, TypeScript.Tracing.Tracing? tracing = null)
    {
        this.program = program;
        this.tracing = tracing;
        long count = program.SingleThreaded() ? 1 : program.Options().Checkers?.Value ?? 4;
        count = Math.Max(Math.Min(Math.Min(count, program.files.Count), 256), 1);
        checkers = new Checker[(int)count];
        locks = new SemaphoreSlim[(int)count];
        createCheckersOnce = new(() =>
        {
            createCheckersWorker();
            return true;
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    internal static CheckerPool newCheckerPool(Program program) => new(program);
    internal static CheckerPool newCheckerPoolWithTracing(Program program, TypeScript.Tracing.Tracing? tracing) => new(program, tracing);
    // Go int is 64-bit on the pinned target. Only bounded storage indices use int.
    internal static long getCheckerAssociationBaseWeight(long nodeCount, long textLength) => Math.Max(unchecked(nodeCount + textLength / checkerAssociationTextWeightDivisor), 1);
    internal static bool shouldPrioritizeSourceFiles(long totalWeight, long declarationWeight, long checkerCount) => unchecked(declarationWeight * checkerCount * 2) <= totalWeight;
    internal static CheckerAssociationPolicy getCheckerAssociationPolicy(long totalWeight, long declarationWeight, long checkerCount)
    {
        if (shouldPrioritizeSourceFiles(totalWeight, declarationWeight, checkerCount))
            return new(true, 1, checkerAssociationPrioritizedSourcePenalty);
        if (checkerCount >= checkerAssociationStrongBalanceMinCheckerCount)
            return new(false, checkerAssociationSourceFileWeightMultiplier, checkerAssociationBalancePenaltyMultiplier);
        return new(false, 1, 1);
    }

    internal static List<long> getCheckerAssociationWeights(List<long>? baseWeights, List<long>? importCounts)
    {
        long totalBaseWeight = 0, totalImports = 0;
        for (int i = 0; i < (baseWeights?.Count ?? 0); i++)
        {
            totalBaseWeight = unchecked(totalBaseWeight + baseWeights![i]);
            totalImports = unchecked(totalImports + importCounts![i]);
        }

        long importWeight = totalImports > 0 ? Math.Max(totalBaseWeight / totalImports, 1) : 0;
        var weights = new List<long>((baseWeights?.Count ?? 0));
        for (int i = 0; i < (baseWeights?.Count ?? 0); i++)
            weights.Add(unchecked(baseWeights![i] + importCounts![i] * importWeight));
        return weights;
    }

    internal static List<int>? getCheckerAssociationOrder(List<long>? weights, List<bool>? declarations, bool prioritizeSourceFiles)
    {
        if (!prioritizeSourceFiles)
            return null;
        var order = Enumerable.Range(0, (weights?.Count ?? 0)).ToList();
        order.Sort((left, right) => declarations![left] != declarations![right] ? (declarations![left] ? 1 : -1) : weights![left] != weights![right] ? (weights![left] > weights![right] ? -1 : 1) : left.CompareTo(right));
        return order;
    }

    internal static List<int>? getCheckerAssociationsInOrder(List<long>? weights, List<List<int>?>? adjacentFiles, List<int>? order, int checkerCount, long penaltyMultiplier)
    {
        if ((weights?.Count ?? 0) == 0)
            return null;
        long totalWeight = 0, maxFileWeight = 0, edgeCount = 0;
        for (int i = 0; i < (weights?.Count ?? 0); i++)
        {
            totalWeight = unchecked(totalWeight + weights![i]);
            maxFileWeight = Math.Max(maxFileWeight, weights![i]);
            edgeCount = unchecked(edgeCount + (adjacentFiles![i]?.Count ?? 0));
        }

        var associations = Enumerable.Repeat(-1, (weights?.Count ?? 0)).ToList();
        var checkerWeights = new long[checkerCount];
        long average = unchecked(totalWeight + checkerCount - 1) / checkerCount;
        long maxWeight = Math.Max(maxFileWeight, unchecked(average + average / 100));
        double total = totalWeight;
        double alpha = penaltyMultiplier * (double)(edgeCount / 2) * Math.Sqrt(checkerCount) / (total * Math.Sqrt(total));
        var neighbors = new long[checkerCount];
        for (int position = 0; position < (weights?.Count ?? 0); position++)
        {
            int file = order is null ? position : order[position];
            Array.Clear(neighbors);
            foreach (int adjacent in adjacentFiles![file] ?? [])
                if (associations[adjacent] >= 0)
                    neighbors[associations[adjacent]]++;
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int checker = 0; checker < checkerCount; checker++)
            {
                if (unchecked(checkerWeights[checker] + weights![file]) > maxWeight)
                    continue;
                double oldWeight = checkerWeights[checker], newWeight = unchecked(checkerWeights[checker] + weights![file]);
                double score = neighbors[checker] - alpha * (newWeight * Math.Sqrt(newWeight) - oldWeight * Math.Sqrt(oldWeight));
                if (score > bestScore || score == bestScore && (best < 0 || checkerWeights[checker] < checkerWeights[best]))
                {
                    best = checker;
                    bestScore = score;
                }
            }

            if (best < 0)
            {
                best = 0;
                for (int checker = 1; checker < checkerCount; checker++)
                    if (checkerWeights[checker] < checkerWeights[best])
                        best = checker;
            }

            associations[file] = best;
            checkerWeights[best] = unchecked(checkerWeights[best] + weights![file]);
        }

        return associations;
    }

    public (Checker Checker, Action Done) GetChecker(CancellationToken context, SourceFile? file)
    {
        if (file is not null)
            return getCheckerForFileExclusive(context, file);
        createCheckers();
        locks[0].Wait();
        return (checkers[0], releaseOnce(locks[0]));
    }

    internal (Checker Checker, Action Done) getCheckerForFileNonExclusive(SourceFile file)
    {
        createCheckers();
        return (fileAssociations.GetValueOrDefault(file)!, noop);
    }

    internal (Checker Checker, Action Done) getCheckerNonExclusive()
    {
        createCheckers();
        return (checkers[0], noop);
    }

    internal (Checker Checker, Action Done) getCheckerForFileExclusive(CancellationToken context, SourceFile file)
    {
        createCheckers();
        Checker checker = fileAssociations.GetValueOrDefault(file)!;
        int index = Array.IndexOf(checkers, checker);
        locks[index].Wait();
        return (checker, releaseOnce(locks[index]));
    }

    private static Action releaseOnce(SemaphoreSlim gate)
    {
        int released = 0;
        return () =>
        {
            if (Interlocked.Exchange(ref released, 1) == 0)
                gate.Release();
        };
    }

    internal static void noop()
    {
    }

    internal void createCheckers() => _ = createCheckersOnce.Value;
    private void createCheckersWorker()
    {
        int checkerCount = checkers.Length;
        var work = Core.NewWorkGroup(program.SingleThreaded());
        for (int i = 0; i < checkerCount; i++)
        {
            int index = i;
            work.Queue(() =>
            {
                if (tracing is not null)
                    throw new UnsupportedSemanticFeatureException("checker type tracing is outside Program construction");
                checkers[index] = program.CreateActualChecker();
                locks[index] = checkers[index].mu;
            });
        }

        work.RunAndWait();
        List<int> associations = Enumerable.Repeat(0, program.files.Count).ToList();
        if (checkerCount > 1)
        {
            var baseWeights = new List<long>();
            var importCounts = new List<long>();
            var declarations = new List<bool>();
            long totalBaseWeight = 0, declarationBaseWeight = 0;
            foreach (SourceFile file in program.files)
            {
                long weight = getCheckerAssociationBaseWeight(file.NodeCount, file.TextView().Length);
                totalBaseWeight = unchecked(totalBaseWeight + weight);
                if (file.IsDeclarationFile)
                    declarationBaseWeight = unchecked(declarationBaseWeight + weight);
                baseWeights.Add(weight);
                importCounts.Add(file.Imports()?.Count ?? 0);
                declarations.Add(file.IsDeclarationFile);
            }

            var policy = getCheckerAssociationPolicy(totalBaseWeight, declarationBaseWeight, checkerCount);
            if (policy.sourceFileWeightMultiplier != 1)
                for (int i = 0; i < declarations.Count; i++)
                    if (!declarations[i])
                        baseWeights![i] = unchecked(baseWeights![i] * policy.sourceFileWeightMultiplier);
            var weights = getCheckerAssociationWeights(baseWeights, importCounts);
            associations = getCheckerAssociationsInOrder(weights, getImportAdjacency(), getCheckerAssociationOrder(weights, declarations, policy.prioritizeSourceFiles), checkerCount, policy.balancePenaltyMultiplier)!;
        }

        fileAssociations = new(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < program.files.Count; i++)
            fileAssociations[program.files[i]] = checkers[associations[i]];
    }

    internal List<List<int>?> getImportAdjacency()
    {
        var indices = new Dictionary<SourceFile, int>(ReferenceEqualityComparer.Instance);
        for (int i = 0; i < program.files.Count; i++)
            indices[program.files[i]] = i;
        var adjacency = Enumerable.Repeat<List<int>?>(null, program.files.Count).ToList();
        for (int i = 0; i < program.files.Count; i++)
        {
            var modules = program.resolvedModules.GetValueOrDefault(program.files[i].PathKey());
            if (modules is null)
                continue;
            foreach (var resolved in modules.Values)
            {
                if (!resolved.IsResolved())
                    continue;
                SourceFile? target = program.GetSourceFileForResolvedModule(resolved!);
                if (target is null || !indices.TryGetValue(target, out int index) || index == i)
                    continue;
                (adjacency[i] ??= []).Add(index);
                (adjacency[index] ??= []).Add(i);
            }
        }

        return adjacency;
    }

    internal void forEachCheckerParallel(Action<int, Checker> callback)
    {
        createCheckers();
        var work = Core.NewWorkGroup(program.SingleThreaded());
        for (int i = 0; i < checkers.Length; i++)
        {
            int index = i;
            work.Queue(() =>
            {
                locks[index].Wait();
                try
                {
                    callback(index, checkers[index]);
                }
                finally
                {
                    locks[index].Release();
                }
            });
        }

        work.RunAndWait();
    }

    internal List<Diagnostic>? GetGlobalDiagnostics()
    {
        createCheckers();
        var diagnostics = new List<Diagnostic>? [checkers.Length];
        forEachCheckerParallel((index, checker) => diagnostics[index] = checker.GetGlobalDiagnostics());
        // slices.Concat returns nil when the total length is zero.
        List<Diagnostic>? combined = null;
        foreach (var list in diagnostics)
            if (list is { Count: > 0 })
                (combined ??= []).AddRange(list);
        return Program.SortAndDeduplicateDiagnostics(combined);
    }

    internal void forEachCheckerGroupDo(CancellationToken context, List<SourceFile> files, bool singleThreaded, Action<Checker, int, SourceFile> callback)
    {
        createCheckers();
        var work = Core.NewWorkGroup(singleThreaded);
        for (int i = 0; i < checkers.Length; i++)
        {
            int index = i;
            work.Queue(() =>
            {
                locks[index].Wait();
                try
                {
                    for (int file = 0; file < files.Count; file++)
                        if (ReferenceEquals(checkers[index], fileAssociations.GetValueOrDefault(files[file])))
                            callback(checkers[index], file, files[file]);
                }
                finally
                {
                    locks[index].Release();
                }
            });
        }

        work.RunAndWait();
    }
}
