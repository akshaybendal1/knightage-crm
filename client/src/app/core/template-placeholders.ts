export interface TemplatePlaceholderValues {
  leadName: string;
  yourName: string;
}

const PLACEHOLDER_PATTERN = /\{\{\s*(\w+)\s*\}\}/g;

/**
 * Unknown tokens are left untouched rather than blanked, so a typo like
 * {{ldName}} doesn't silently send a broken message.
 */
export function applyTemplatePlaceholders(body: string, values: TemplatePlaceholderValues): string {
  return body.replace(PLACEHOLDER_PATTERN, (match, token: string) =>
    token in values ? values[token as keyof TemplatePlaceholderValues] : match,
  );
}
