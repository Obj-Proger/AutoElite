// Runs the renewal of the session in one tab of the browser at a time, with the Web Locks API: the tabs share the
// cookie of the session, whose refresh token the API accepts only once. A browser without the API runs it at once.
export function runExclusively(name, renewal) {
    if (!navigator.locks) {
        return renewal.invokeMethodAsync("Run");
    }

    return navigator.locks.request(name, () => renewal.invokeMethodAsync("Run"));
}
