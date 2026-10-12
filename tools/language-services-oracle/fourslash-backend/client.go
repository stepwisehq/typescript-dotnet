// Additive original-test client constructor. No Go Server/Session is created.
package lsptestutil

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"github.com/microsoft/TypeScript/tsc/internal/core"
	"github.com/microsoft/TypeScript/tsc/internal/jsonrpc"
	"github.com/microsoft/TypeScript/tsc/internal/lsp"
	"github.com/microsoft/TypeScript/tsc/internal/lsp/lsproto"
	"golang.org/x/sync/errgroup"
	"io"
	"net"
	"os"
	"os/exec"
	"path/filepath"
	"strconv"
	"strings"
	"sync"
	"testing"
)

// Bounded validation lifecycle evidence; never enters framed protocol stdout.
func candidateOutcome(err error) string {
	if err == nil {
		return "success"
	}
	text := err.Error()
	if len(text) > 4096 {
		return text[:4096] + " [truncated]"
	}
	return text
}

// Validation-only failure evidence; stdout remains exclusively framed LSP.
// Preserve raw child bytes and bound storage even for a failing noisy process.
type candidateFailureStderr struct {
	mu   sync.Mutex
	data []byte
}

func (w *candidateFailureStderr) Write(data []byte) (int, error) {
	w.mu.Lock()
	defer w.mu.Unlock()
	n := len(data)
	remaining := (64 << 10) - len(w.data)
	if len(data) > remaining {
		data = data[:remaining]
	}
	w.data = append(w.data, data...)
	return n, nil
}
func (w *candidateFailureStderr) bytes() []byte {
	w.mu.Lock()
	defer w.mu.Unlock()
	return append([]byte(nil), w.data...)
}

type candidateBackend struct{ control *candidateControl }

func newCandidateLSPClient(t *testing.T, opts lsp.ServerOptions, onRequest ServerRequestHandler) (*LSPClient, func() error) {
	listener, err := net.Listen("tcp", "127.0.0.1:0")
	if err != nil {
		t.Fatal(err)
	}
	address := listener.Addr().(*net.TCPAddr)
	ctx, cancelCause := context.WithCancelCause(t.Context())
	cancel := func() { cancelCause(context.Canceled) }
	group, groupCtx := errgroup.WithContext(ctx)
	started := false
	var stdin io.WriteCloser
	var stdout io.ReadCloser
	defer func() {
		if !started {
			cancel()
			listener.Close()
			if stdin != nil {
				stdin.Close()
			}
			if stdout != nil {
				stdout.Close()
			}
		}
	}()
	executable := os.Getenv("TSDOTNET_FOURSLASH_SERVER")
	args := []string{"--fourslash-host", "127.0.0.1", strconv.Itoa(address.Port)}
	if strings.EqualFold(filepath.Ext(executable), ".dll") {
		args = append([]string{executable}, args...)
		executable = "dotnet"
	}
	command := exec.CommandContext(groupCtx, executable, args...)
	stdin, err = command.StdinPipe()
	if err != nil {
		t.Fatal(err)
	}
	stdout, err = command.StdoutPipe()
	if err != nil {
		t.Fatal(err)
	}
	stderr := &candidateFailureStderr{}
	command.Stderr = stderr
	if opts.Err != nil {
		command.Stderr = io.MultiWriter(stderr, opts.Err)
	}
	if err = command.Start(); err != nil {
		t.Fatal(err)
	}
	started = true
	processExit := make(chan error, 1)
	processDone := make(chan struct{})
	// Constructor setup can panic before the original caller registers closeClient.
	// Own cancellation/reaping immediately; all later cleanup is idempotent.
	t.Cleanup(func() {
		cancel()
		listener.Close()
		stdin.Close()
		<-processDone
	})
	go func() {
		defer close(processDone)
		err := command.Wait()
		fmt.Fprintf(os.Stderr, "candidate child Wait outcome: %s\n", candidateOutcome(err))
		if captured := stderr.bytes(); len(captured) != 0 {
			// The original test may already be unwinding from control EOF.
			// Parent stderr preserves this evidence even after testing.T cleanup.
			fmt.Fprintf(os.Stderr, "candidate child stderr (first 64KiB):\n%s\n", captured)
			if err != nil {
				err = fmt.Errorf("%w; candidate stderr: %s", err, captured)
			}
		}
		processExit <- err
		listener.Close()
	}()
	// Accept unblocks when process exits or the original test context cancels.
	go func() { <-groupCtx.Done(); listener.Close() }()
	socket, err := listener.Accept()
	listener.Close()
	if err != nil {
		cancel()
		<-processExit
		t.Fatal(err)
	}
	processes := &candidateProcesses{}
	var control *candidateControl
	control = newCandidateControl(socket, func(operation string, raw json.RawMessage) (json.RawMessage, *string) {
		switch operation {
		case "host.info":
			return candidateJSON(map[string]any{"caseSensitivity": int(opts.FS.CaseSensitivity()), "cwd": candidateEncode(string(opts.Cwd)), "defaultLibraryPath": candidateEncode(string(opts.DefaultLibraryPath)), "typingsLocation": candidateEncode(string(opts.TypingsLocation)), "hasSpawner": opts.Spawn != nil, "hasNpmInstaller": opts.NpmInstall != nil, "hasParentProcessCallback": opts.SetParentProcessID != nil, "progressTicks": int64(opts.ProgressDelay) / 100})
		case "host.npmInstall":
			var args struct {
				Directory string   `json:"directory"`
				Arguments []string `json:"arguments"`
			}
			if err := json.Unmarshal(raw, &args); err != nil {
				return candidateFailure(err)
			}
			dir, err := candidateDecode(args.Directory)
			if err != nil {
				return candidateFailure(err)
			}
			arguments := make([]string, len(args.Arguments))
			for i, value := range args.Arguments {
				arguments[i], err = candidateDecode(value)
				if err != nil {
					return candidateFailure(err)
				}
			}
			if opts.NpmInstall == nil {
				return candidateFailure(errors.New("original host has no injected npm installer"))
			}
			output, err := opts.NpmInstall(groupCtx, dir, arguments)
			var failure *string
			if err != nil {
				s := candidateEncode(err.Error())
				failure = &s
			}
			return candidateJSON(map[string]any{"output": candidateEncode(string(output)), "error": failure})
		case "host.parentProcess":
			var args struct {
				PID int `json:"pid"`
			}
			if err := json.Unmarshal(raw, &args); err != nil {
				return candidateFailure(err)
			}
			if opts.SetParentProcessID == nil {
				return candidateFailure(errors.New("original host has no parent-process callback"))
			}
			opts.SetParentProcessID(args.PID)
			return candidateJSON(nil)
		case "process.spawn":
			var args struct {
				Command      []string `json:"command"`
				Directory    string   `json:"directory"`
				StderrHandle int64    `json:"stderrHandle"`
			}
			if err := json.Unmarshal(raw, &args); err != nil {
				return candidateFailure(err)
			}
			command := make([]string, len(args.Command))
			for i, part := range args.Command {
				value, err := candidateDecode(part)
				if err != nil {
					return candidateFailure(err)
				}
				command[i] = value
			}
			dir, err := candidateDecode(args.Directory)
			if err != nil {
				return candidateFailure(err)
			}
			if opts.Spawn == nil {
				return candidateFailure(errors.New("original host has no injected spawner"))
			}
			stream, err := opts.Spawn(command, dir, candidateStderr{control, args.StderrHandle})
			if err != nil {
				return candidateFailure(err)
			}
			return candidateJSON(processes.add(stream))
		}
		if strings.HasPrefix(operation, "fs.") {
			return candidateFSDispatch(opts.FS, operation, raw)
		}
		if strings.HasPrefix(operation, "process.") {
			return processes.dispatch(operation, raw)
		}
		return candidateFailure(fmt.Errorf("unknown candidate host operation %q", operation))
	})
	client := &LSPClient{candidate: &candidateBackend{control}, inputWriter: lsp.ToWriter(stdin), outputReader: lsp.ToReader(stdout), pendingRequests: make(map[jsonrpc.ID]chan *lsproto.ResponseMessage), onServerRequest: onRequest, ctx: groupCtx}
	group.Go(func() error {
		// EOF/error terminates the candidate transport, including outstanding requests.
		err := client.MessageRouter(groupCtx)
		if err != nil {
			cancelCause(err)
		} else {
			cancelCause(io.EOF)
		}
		fmt.Fprintf(os.Stderr, "candidate message router outcome: %s\n", candidateOutcome(err))
		return err
	})
	group.Go(func() error {
		err := <-processExit
		if err != nil {
			cancelCause(err)
		} else {
			cancelCause(io.EOF)
		}
		control.fail(io.EOF)
		if groupCtx.Err() != nil {
			return groupCtx.Err()
		}
		cancel()
		return err
	})
	var closeOnce sync.Once
	var closeError error
	closeClient := func() error {
		closeOnce.Do(func() {
			cancel()
			_ = stdin.Close()
			controlErr := control.close()
			processErr := processes.close()
			runErr := group.Wait()
			if errors.Is(runErr, context.Canceled) {
				runErr = nil
			}
			closeError = errors.Join(controlErr, processErr, runErr)
		})
		return closeError
	}
	// Registered before beforeInitialize/initialize can fail; original returned
	// cleanup uses the same owner and does not double-close process resources.
	t.Cleanup(func() {
		if err := closeClient(); err != nil {
			fmt.Fprintf(os.Stderr, "candidate cleanup outcome: %s\n", candidateOutcome(err))
		}
	})
	return client, closeClient
}

type candidateStderr struct {
	control *candidateControl
	handle  int64
}

func (w candidateStderr) Write(data []byte) (int, error) {
	response, err := w.control.call("process.stderr", map[string]any{"handle": w.handle, "bytes": candidateEncode(string(data))})
	if err != nil {
		return 0, err
	}
	if response.ErrorBase64 != nil {
		raw, _ := candidateDecode(*response.ErrorBase64)
		return 0, errors.New(raw)
	}
	var n int
	err = json.Unmarshal(response.Result, &n)
	return n, err
}
func (c *LSPClient) CandidateBackend() bool { return c.candidate != nil }
func (c *LSPClient) WaitInitComplete() {
	if c.candidate == nil {
		<-c.Server.InitComplete()
		return
	}
	response, err := c.candidate.control.call("server.initComplete", struct{}{})
	candidateMust(response, err)
}
func (c *LSPClient) CandidateStateDiff(initialized bool, files []string) string {
	encoded := make([]string, len(files))
	for i, file := range files {
		encoded[i] = candidateEncode(file)
	}
	response, err := c.candidate.control.call("server.stateDiff", map[string]any{"initialized": initialized, "openFiles": encoded})
	candidateMust(response, err)
	var output string
	if err = json.Unmarshal(response.Result, &output); err != nil {
		panic(err)
	}
	raw, err := candidateDecode(output)
	if err != nil {
		panic(err)
	}
	return raw
}
func candidateMust(response candidateControlMessage, err error) {
	if err != nil {
		panic(err)
	}
	if response.ErrorBase64 != nil {
		raw, _ := candidateDecode(*response.ErrorBase64)
		panic(raw)
	}
}
func (c *LSPClient) candidateSetOptions(options *core.CompilerOptions) {
	raw, err := json.Marshal(options)
	if err != nil {
		panic(err)
	}
	response, err := c.candidate.control.call("server.inferredOptions", map[string]any{"options": candidateEncode(string(raw))})
	candidateMust(response, err)
}
