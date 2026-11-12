'use strict';

const assert = require('node:assert/strict');
const test = require('node:test');
const { mediaUploadPath } = require('./media-upload');

test('routes cell background images through thumbnail optimization', () => {
  assert.equal(mediaUploadPath('image'), '/api/v1/media-assets/thumbnails');
});

test('retains the generic media upload endpoint for playable media', () => {
  assert.equal(mediaUploadPath('any'), '/api/v1/media-assets');
});
