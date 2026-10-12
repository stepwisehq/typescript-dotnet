// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See licenses/TypeScript-LICENSE.txt.
// Linux x64/glibc representation adaptation for checker.go:mu and checkerpool.go:locks.
// Preserves Go sync.Mutex nonreentrancy and cross-thread Unlock; no custom wait queue.
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace TypeScript.Scanner;
internal sealed unsafe partial class CheckerMutex
{
    private const int Interrupted = 4;
    private static readonly bool nativeSupported = DetectNativeSupport();
    private readonly SemaphoreSlim? fallback;
    private nint semaphore;
    private int held;
    internal bool UsesNativeSemaphore => semaphore != 0;

    // Forced fallback is for the shared platform-control assertion, not a runtime knob.
    internal CheckerMutex(bool forceFallback = false)
    {
        if (forceFallback || !nativeSupported)
        {
            fallback = new SemaphoreSlim(1, 1);
            return;
        }

        // glibc Linux x64 sem_t is 32 bytes, aligned to long (8 bytes).
        nint storage = (nint)NativeMemory.AllocZeroed(32);
        if (storage == 0)
            throw new OutOfMemoryException();
        try
        {
            if (SemInit(storage, 0, 1) != 0)
                throw NativeFailure("sem_init", Marshal.GetLastPInvokeError());
            semaphore = storage; // Finalizer may destroy only successfully initialized state.
        }
        finally
        {
            if (semaphore == 0)
                NativeMemory.Free((void*)storage);
        }
    }

    internal void Wait()
    {
        if (fallback is not null)
        {
            fallback.Wait();
            return;
        }

        try
        {
            while (SemWait(semaphore) != 0)
            {
                int error = Marshal.GetLastPInvokeError();
                if (error != Interrupted)
                    throw NativeFailure("sem_wait", error);
            }

            Volatile.Write(ref held, 1);
        }
        finally
        {
            // A parked native call must keep its stable semaphore owner alive.
            GC.KeepAlive(this);
        }
    }

    internal void Release()
    {
        if (fallback is not null)
        {
            fallback.Release();
            return;
        }

        try
        {
            // Clear BEFORE posting: the awakened acquirer may immediately set held.
            // POSIX itself permits counts above one; preserve the previous binary guard.
            if (Interlocked.CompareExchange(ref held, 0, 1) != 1)
                throw new SemaphoreFullException();
            if (SemPost(semaphore) != 0)
            {
                int error = Marshal.GetLastPInvokeError();
                Volatile.Write(ref held, 1);
                throw NativeFailure("sem_post", error);
            }
        }
        finally
        {
            GC.KeepAlive(this);
        }
    }

    ~CheckerMutex()
    {
        // No Dispose surface: active Wait/Release and transferable Done leases retain
        // this owner. Never destroy/free an initialized semaphore with an active waiter.
        // Finalization cannot throw; on unexpected destroy failure retain its storage.
        try
        {
            if (semaphore != 0 && SemDestroy(semaphore) == 0)
                NativeMemory.Free((void*)semaphore);
        }
        catch
        {
        // Retain storage rather than throwing from finalization or freeing live state.
        }
    }

    private static bool DetectNativeSupport()
    {
        if (!OperatingSystem.IsLinux() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            return false;
        if (!NativeLibrary.TryLoad("libc.so.6", out nint library))
            return false;
        try
        {
            // Before glibc2.34 these symbols may exist only in libpthread. Select
            // fallback before initialization instead of assuming every glibc exports them.
            return NativeLibrary.TryGetExport(library, "gnu_get_libc_version", out _) && NativeLibrary.TryGetExport(library, "sem_init", out _) && NativeLibrary.TryGetExport(library, "sem_wait", out _) && NativeLibrary.TryGetExport(library, "sem_post", out _) && NativeLibrary.TryGetExport(library, "sem_destroy", out _);
        }
        finally
        {
            NativeLibrary.Free(library);
        }
    }

    private static InvalidOperationException NativeFailure(string operation, int error) => new($"{operation} failed: errno {error}", new Win32Exception(error));
    [LibraryImport("libc.so.6", EntryPoint = "sem_init", SetLastError = true)]
    private static partial int SemInit(nint semaphore, int processShared, uint value);
    // Blocking calls must retain the normal GC transition.
    [LibraryImport("libc.so.6", EntryPoint = "sem_wait", SetLastError = true)]
    private static partial int SemWait(nint semaphore);
    [LibraryImport("libc.so.6", EntryPoint = "sem_post", SetLastError = true)]
    private static partial int SemPost(nint semaphore);
    [LibraryImport("libc.so.6", EntryPoint = "sem_destroy", SetLastError = true)]
    private static partial int SemDestroy(nint semaphore);
}
