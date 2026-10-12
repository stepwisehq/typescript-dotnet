package lsptestutil

import (
	"encoding/base64"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"sync"
)

type candidateProcess struct {
	stream           io.ReadWriteCloser
	readMu           sync.Mutex
	pendingReadError error
}
type candidateProcesses struct {
	mu    sync.Mutex
	next  int64
	items map[int64]*candidateProcess
}

func (p *candidateProcesses) add(stream io.ReadWriteCloser) int64 {
	p.mu.Lock()
	defer p.mu.Unlock()
	if p.items == nil {
		p.items = make(map[int64]*candidateProcess)
	}
	p.next++
	p.items[p.next] = &candidateProcess{stream: stream}
	return p.next
}
func (p *candidateProcesses) dispatch(operation string, raw json.RawMessage) (json.RawMessage, *string) {
	var args struct {
		Handle int64  `json:"handle"`
		Count  int    `json:"count"`
		Bytes  string `json:"bytes"`
	}
	if err := json.Unmarshal(raw, &args); err != nil {
		return candidateFailure(err)
	}
	p.mu.Lock()
	process := p.items[args.Handle]
	p.mu.Unlock()
	if process == nil {
		return candidateFailure(fmt.Errorf("unknown candidate process %d", args.Handle))
	}
	switch operation {
	case "process.read":
		if args.Count < 0 {
			return candidateFailure(errors.New("negative process read length"))
		}
		process.readMu.Lock()
		defer process.readMu.Unlock()
		var data []byte
		var err error
		if process.pendingReadError != nil {
			err = process.pendingReadError
			process.pendingReadError = nil
		} else {
			data = make([]byte, args.Count)
			var n int
			n, err = process.stream.Read(data)
			data = data[:n]
			if n > 0 && err != nil {
				process.pendingReadError = err
				err = nil
			}
		}
		var failure *string
		if err != nil && !errors.Is(err, io.EOF) {
			s := candidateEncode(err.Error())
			failure = &s
		}
		return candidateJSON(map[string]any{"bytes": base64.StdEncoding.EncodeToString(data), "error": failure})
	case "process.write":
		data, err := base64.StdEncoding.DecodeString(args.Bytes)
		if err != nil {
			return candidateFailure(err)
		}
		n, err := process.stream.Write(data)
		var failure *string
		if err != nil {
			s := candidateEncode(err.Error())
			failure = &s
		}
		return candidateJSON(map[string]any{"count": n, "error": failure})
	case "process.close":
		p.mu.Lock()
		delete(p.items, args.Handle)
		p.mu.Unlock()
		if err := process.stream.Close(); err != nil {
			return candidateFailure(err)
		}
		return candidateJSON(nil)
	default:
		return candidateFailure(fmt.Errorf("unknown process operation %q", operation))
	}
}
func (p *candidateProcesses) close() error {
	p.mu.Lock()
	items := p.items
	p.items = nil
	p.mu.Unlock()
	var failures []error
	for _, process := range items {
		if err := process.stream.Close(); err != nil {
			failures = append(failures, err)
		}
	}
	return errors.Join(failures...)
}
