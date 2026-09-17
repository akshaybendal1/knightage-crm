/**
 * Builds a WhatsApp click-to-chat link (https://wa.me/<number>), or null when there's
 * no usable phone number to link to -- callers use that to hide the button entirely
 * rather than render a link to nothing.
 *
 * wa.me needs the full number, digits only, with country code. Phone numbers in this
 * app are free text (the input only restricts to digits/+/-/spaces/parens, it doesn't
 * enforce a country code), so two common no-country-code shapes are assumed to be an
 * Indian mobile number and get "91" prefixed: a bare 10-digit number, and a 10-digit
 * number with a leading trunk "0" (11 digits total -- how people often type a domestic
 * number out of landline-dialing habit, even for mobiles). Anything else (already has
 * a country code, or an unusual length) is passed through as-is; this is a best-effort
 * default, not a full international validator.
 */
export function buildWhatsAppLink(phone: string | null | undefined, message?: string): string | null {
  if (!phone) {
    return null;
  }

  const digits = phone.replace(/\D/g, '');
  if (!digits) {
    return null;
  }

  let withCountryCode = digits;
  if (digits.length === 10) {
    withCountryCode = `91${digits}`;
  } else if (digits.length === 11 && digits.startsWith('0')) {
    withCountryCode = `91${digits.slice(1)}`;
  }

  const base = `https://wa.me/${withCountryCode}`;
  return message ? `${base}?text=${encodeURIComponent(message)}` : base;
}
