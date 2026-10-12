// Authored candidate-backend host control; compiled only through the additive oracle overlay.
package lsptestutil

import (
	"bufio"
	"encoding/json"
	"errors"
	"fmt"
	"io"
	"os"
	"sync"
	"sync/atomic"
)

type candidateControlMessage struct {
	ID          int64           `json:"id"`
	Operation   string          `json:"operation,omitempty"`
	Arguments   json.RawMessage `json:"arguments,omitempty"`
	Result      json.RawMessage `json:"result,omitempty"`
	ErrorBase64 *string         `json:"errorBase64,omitempty"`
	Response    bool            `json:"response"`
}

type candidateControl struct {
	stream   io.ReadWriteCloser
	writeMu  sync.Mutex
	mu       sync.Mutex
	pending  map[int64]chan candidateControlMessage
	terminal error
	next     atomic.Int64
	dispatch func(string, json.RawMessage) (json.RawMessage, *string)
	done     chan struct{}
}

func newCandidateControl(stream io.ReadWriteCloser, dispatch func(string, json.RawMessage) (json.RawMessage, *string)) *candidateControl {
	c := &candidateControl{stream: stream, pending: make(map[int64]chan candidateControlMessage), dispatch: dispatch, done: make(chan struct{})}
	go c.read()
	return c
}
func (c *candidateControl) send(m candidateControlMessage) error {
	c.writeMu.Lock()
	defer c.writeMu.Unlock()
	return json.NewEncoder(c.stream).Encode(m)
}
func (c *candidateControl) call(operation string, args any) (candidateControlMessage, error) {
	raw, err := json.Marshal(args)
	if err != nil {
		return candidateControlMessage{}, err
	}
	id := c.next.Add(1)
	ch := make(chan candidateControlMessage, 1)
	c.mu.Lock()
	if c.terminal != nil {
		err = c.terminal
		c.mu.Unlock()
		return candidateControlMessage{}, err
	}
	c.pending[id] = ch
	c.mu.Unlock()
	defer func() { c.mu.Lock(); delete(c.pending, id); c.mu.Unlock() }()
	if err = c.send(candidateControlMessage{ID: id, Operation: operation, Arguments: raw}); err != nil {
		c.fail(err)
		return candidateControlMessage{}, err
	}
	select {
	case response := <-ch:
		return response, nil
	case <-c.done:
		c.mu.Lock()
		err = c.terminal
		c.mu.Unlock()
		return candidateControlMessage{}, err
	}
}
func (c *candidateControl) read() {
	// Reader has no token-size ceiling: original filesystem reads include bundled files.
	decoder := json.NewDecoder(bufio.NewReader(c.stream))
	for {
		var m candidateControlMessage
		if err := decoder.Decode(&m); err != nil {
			c.fail(err)
			return
		}
		if m.Response {
			c.mu.Lock()
			ch := c.pending[m.ID]
			c.mu.Unlock()
			if ch == nil {
				c.fail(fmt.Errorf("unknown candidate control response %d", m.ID))
				return
			}
			select {
			case ch <- m:
			default:
				c.fail(errors.New("duplicate candidate control response"))
				return
			}
		} else {
			go func() {
				result, failure := c.dispatch(m.Operation, m.Arguments)
				if err := c.send(candidateControlMessage{ID: m.ID, Result: result, ErrorBase64: failure, Response: true}); err != nil {
					c.fail(err)
				}
			}()
		}
	}
}
func (c *candidateControl) fail(err error) {
	c.mu.Lock()
	defer c.mu.Unlock()
	if c.terminal != nil {
		return
	}
	c.terminal = err
	fmt.Fprintf(os.Stderr, "candidate control terminal outcome: %s\n", candidateOutcome(err))
	close(c.done)
}
func (c *candidateControl) close() error { c.fail(io.ErrClosedPipe); return c.stream.Close() }
