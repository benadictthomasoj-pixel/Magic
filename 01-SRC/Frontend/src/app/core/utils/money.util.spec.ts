import { addPaise, formatPaise, subPaise, toBigIntPaise } from './money.util';

describe('Money Utilities (64-Bit Integer Paise)', () => {
  describe('toBigIntPaise', () => {
    it('should parse number correctly', () => {
      expect(toBigIntPaise(10050)).toBe(10050n);
    });

    it('should handle null, undefined, NaN', () => {
      expect(toBigIntPaise(null)).toBe(0n);
      expect(toBigIntPaise(undefined)).toBe(0n);
      expect(toBigIntPaise(NaN)).toBe(0n);
    });

    it('should parse string number', () => {
      expect(toBigIntPaise('295000')).toBe(295000n);
    });
  });

  describe('formatPaise', () => {
    it('should format zero correctly', () => {
      expect(formatPaise(0)).toBe('₹0.00');
    });

    it('should format small amount', () => {
      expect(formatPaise(50)).toBe('₹0.50');
    });

    it('should format standard amount in Indian numbering format', () => {
      expect(formatPaise(100000)).toBe('₹1,000.00');
      expect(formatPaise(10000000)).toBe('₹1,00,000.00');
      expect(formatPaise(1234567890)).toBe('₹1,23,45,678.90');
    });

    it('should format negative amounts correctly', () => {
      expect(formatPaise(-25000)).toBe('-₹250.00');
    });
  });

  describe('arithmetic', () => {
    it('should accurately add paise without floating point errors', () => {
      expect(addPaise(10050, 20075)).toBe(30125);
    });

    it('should accurately subtract paise', () => {
      expect(subPaise(50000, 15025)).toBe(34975);
    });
  });
});
