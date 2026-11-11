'use strict';

const assert = require('node:assert/strict');
const { EventEmitter } = require('node:events');
const test = require('node:test');
const { createUpdateService } = require('./update-service');

class FakeUpdater extends EventEmitter {
  async checkForUpdates() {
    this.emit('update-available', { version: '1.1.0' });
  }

  async downloadUpdate() {
    this.emit('download-progress', { percent: 42 });
    this.emit('download-progress', { percent: 100 });
    this.emit('update-downloaded', { version: '1.1.0' });
  }

  quitAndInstall(silent, forceRunAfter) {
    this.installArguments = [silent, forceRunAfter];
  }
}

test('unsigned packaged builds remain fail closed', async () => {
  const updater = new FakeUpdater();
  const service = createUpdateService({ isPackaged: true, getVersion: () => '1.0.0' }, async () => {}, false, updater);

  const status = await service.check();

  assert.equal(status.phase, 'test');
  assert.match(status.message, /cannot download or apply/i);
  await assert.rejects(service.download(), /No verified update/);
});

test('signed release updates progress through download, verification, and manual apply', async () => {
  const updater = new FakeUpdater();
  let prepared = false;
  const service = createUpdateService(
    { isPackaged: true, getVersion: () => '1.0.0' },
    async () => { prepared = true; },
    true,
    updater);

  assert.equal((await service.check()).phase, 'available');
  assert.equal((await service.download()).phase, 'ready');
  await service.apply();

  assert.equal(prepared, true);
  assert.deepEqual(updater.installArguments, [false, true]);
});
