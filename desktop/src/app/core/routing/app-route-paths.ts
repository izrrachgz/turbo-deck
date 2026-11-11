import { AppRouteSegments } from './app-route-constants';

export const AppRoutePaths = Object.fromEntries(
  Object.entries(AppRouteSegments).map(([key, segment]) => [key, `/${segment}`]),
) as Readonly<Record<keyof typeof AppRouteSegments, string>>;
