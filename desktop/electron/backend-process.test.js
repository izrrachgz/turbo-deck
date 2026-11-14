'use strict';

const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const test = require('node:test');
const { portConflictError, startBackendWithRetry, stopBackendProcess } = require('./backend-process');

const bindError = () => new Error(
  'Backend exited during startup. Failed to bind to address https://127.0.0.1:52024: address already in use.',
);

test('retries a transient port conflict during an installed-version handoff', async () => {
  let starts = 0;
  let waits = 0;
  const result = await startBackendWithRetry(async () => {
    starts += 1;
    if (starts < 3) throw bindError();
    return 'ready';
  }, { attempts: 4, retryDelayMilliseconds: 1, wait: async () => { waits += 1; } });

  assert.equal(result, 'ready');
  assert.equal(starts, 3);
  assert.equal(waits, 2);
});

test('reports a concise persistent port conflict without the backend stack trace', async () => {
  await assert.rejects(
    startBackendWithRetry(async () => { throw bindError(); }, {
      attempts: 2,
      retryDelayMilliseconds: 1,
      wait: async () => {},
    }),
    error => {
      assert.match(error.message, /TCP port 52024 is already in use/);
      assert.doesNotMatch(error.message, /Backend exited|Kestrel|stack trace/);
      return true;
    },
  );
  assert.match(portConflictError(bindError()).message, /https:\/\/127\.0\.0\.1:52024/);
});

test('waits for graceful backend exit before completing application shutdown', async () => {
  const child = new FakeChildProcess();

  await stopBackendProcess(child, async () => child.exit(0), {
    gracefulTimeoutMilliseconds: 20,
    forcedTimeoutMilliseconds: 20,
  });

  assert.equal(child.killCount, 0);
  assert.equal(child.exitCode, 0);
});

test('terminates a backend that does not release the port during graceful shutdown', async () => {
  const child = new FakeChildProcess();

  await stopBackendProcess(child, async () => {}, {
    gracefulTimeoutMilliseconds: 1,
    forcedTimeoutMilliseconds: 20,
  });

  assert.equal(child.killCount, 1);
  assert.equal(child.exitCode, 1);
});

class FakeChildProcess extends EventEmitter {
  exitCode = null;
  killCount = 0;

  exit(code) {
    this.exitCode = code;
    this.emit('exit', code);
  }

  kill() {
    this.killCount += 1;
    this.exit(1);
    return true;
  }
}
