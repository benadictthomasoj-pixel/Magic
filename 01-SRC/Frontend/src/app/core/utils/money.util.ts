/**
 * 64-Bit Integer Money Utilities
 * Provides exact arithmetic and Indian currency formatting for integer paise,
 * eliminating JavaScript floating-point rounding inaccuracies.
 */

export function toBigIntPaise(value: number | string | bigint | null | undefined): bigint {
  if (value === null || value === undefined) return 0n;
  if (typeof value === 'bigint') return value;
  if (typeof value === 'number') {
    if (isNaN(value) || !isFinite(value)) return 0n;
    return BigInt(Math.round(value));
  }
  const clean = String(value).trim();
  if (!clean) return 0n;
  try {
    return BigInt(clean);
  } catch {
    const num = Number(clean);
    return isNaN(num) ? 0n : BigInt(Math.round(num));
  }
}

export function formatPaise(paise: number | string | bigint | null | undefined): string {
  const bi = toBigIntPaise(paise);
  const isNegative = bi < 0n;
  const abs = isNegative ? -bi : bi;
  const rupees = abs / 100n;
  const cents = abs % 100n;
  const sign = isNegative ? '-' : '';
  const centsStr = cents.toString().padStart(2, '0');

  const rupeesStr = rupees.toString();
  let formattedRupees = '';
  if (rupeesStr.length <= 3) {
    formattedRupees = rupeesStr;
  } else {
    const lastThree = rupeesStr.substring(rupeesStr.length - 3);
    const rest = rupeesStr.substring(0, rupeesStr.length - 3);
    const withCommas = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
    formattedRupees = `${withCommas},${lastThree}`;
  }
  return `${sign}₹${formattedRupees}.${centsStr}`;
}

export function addPaise(a: number | string | bigint, b: number | string | bigint): number {
  return Number(toBigIntPaise(a) + toBigIntPaise(b));
}

export function subPaise(a: number | string | bigint, b: number | string | bigint): number {
  return Number(toBigIntPaise(a) - toBigIntPaise(b));
}
