export function getDaysBetweenDates(utcDate: Date, localDate = new Date()): number {
  // Normalize both dates to midnight
  const localMidnight = new Date(localDate.getFullYear(), localDate.getMonth(), localDate.getDate());
  const utcMidnight = new Date(Date.UTC(utcDate.getUTCFullYear(), utcDate.getUTCMonth(), utcDate.getUTCDate()));

  const diffInMs = utcMidnight.getTime() - localMidnight.getTime();
  const diffInDays = Math.ceil(diffInMs / (1000 * 60 * 60 * 24));

  return diffInDays;
}

export function getDaysBetweenNowAndFuture(futureDate: Date): number {
  const now = new Date();

  const msInDay = 1000 * 60 * 60 * 24;

  // Get UTC dates to avoid time zone issues
  const utcNow = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());
  const utcFuture = Date.UTC(futureDate.getFullYear(), futureDate.getMonth(), futureDate.getDate());

  const diffInMs = utcFuture - utcNow;

  return Math.ceil(diffInMs / msInDay);
}