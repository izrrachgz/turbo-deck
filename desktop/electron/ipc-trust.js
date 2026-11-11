'use strict';

const { fileURLToPath } = require('node:url');

function assertTrustedSender(event, trustedWebContents) {
  const frame = event.senderFrame;
  if (!trustedWebContents || event.sender !== trustedWebContents || !frame || frame !== event.sender.mainFrame) {
    throw new Error('Blocked IPC from an untrusted frame.');
  }
  try {
    const senderUrl = new URL(frame.url);
    if (senderUrl.protocol !== 'file:') throw new Error('Unexpected renderer protocol.');
    fileURLToPath(senderUrl);
  } catch {
    throw new Error('Blocked IPC from an untrusted renderer.');
  }
}

module.exports = { assertTrustedSender };
