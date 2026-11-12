'use strict';

function mediaUploadPath(mediaKind) {
  return mediaKind === 'image' ? '/api/v1/media-assets/thumbnails' : '/api/v1/media-assets';
}

module.exports = { mediaUploadPath };
