'use strict';

const ADDRESS_IN_USE_PATTERN = /Failed to bind to address\s+(\S+):\s+address already in use/i;

function addressInUseEndpoint(error) {
  const text = error instanceof Error ? error.message : String(error);
  return text.match(ADDRESS_IN_USE_PATTERN)?.[1];
}

function portConflictError(error) {
  const endpoint = addressInUseEndpoint(error);
  if (!endpoint) return error instanceof Error ? error : new Error(String(error));

  let port = '';
  try { port = new URL(endpoint).port; }
  catch { /* Keep the endpoint supplied by Kestrel. */ }
  const portDescription = port ? `TCP port ${port}` : endpoint;
  return new Error(
    `Turbo Deck cannot use ${endpoint} because ${portDescription} is already in use. ` +
    'Quit any Turbo Deck instance still running in the notification area, or close the conflicting application and try again.',
  );
}

async function startBackendWithRetry(start, options = {}) {
  const attempts = options.attempts ?? 4;
  const retryDelayMilliseconds = options.retryDelayMilliseconds ?? 750;
  const wait = options.wait ?? (milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds)));
  let lastError;

  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    try {
      return await start();
    } catch (error) {
      lastError = error;
      if (!addressInUseEndpoint(error)) throw error;
      if (attempt < attempts) await wait(retryDelayMilliseconds);
    }
  }

  throw portConflictError(lastError);
}

function waitForExit(child, timeoutMilliseconds) {
  if (child.exitCode !== null) return Promise.resolve(true);
  return new Promise(resolve => {
    let timer;
    const finish = exited => {
      clearTimeout(timer);
      child.off('exit', onExit);
      resolve(exited);
    };
    const onExit = () => finish(true);
    child.once('exit', onExit);
    timer = setTimeout(() => finish(false), timeoutMilliseconds);
  });
}

async function stopBackendProcess(child, requestShutdown, options = {}) {
  if (!child || child.exitCode !== null) return;
  const gracefulTimeoutMilliseconds = options.gracefulTimeoutMilliseconds ?? 3_000;
  const forcedTimeoutMilliseconds = options.forcedTimeoutMilliseconds ?? 2_000;

  try {
    await requestShutdown();
  } catch {
    if (child.exitCode === null) child.kill();
  }

  if (await waitForExit(child, gracefulTimeoutMilliseconds)) return;
  if (child.exitCode === null) child.kill();
  if (!await waitForExit(child, forcedTimeoutMilliseconds)) {
    throw new Error('The Turbo Deck backend did not exit after it was terminated.');
  }
}

module.exports = { addressInUseEndpoint, portConflictError, startBackendWithRetry, stopBackendProcess };
