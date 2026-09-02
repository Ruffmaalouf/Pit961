import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { SupportEmailLink } from '@/components/support-email-link';
import { makeBranding } from '@/lib/test-utils';
import { resetBrandingStore, useBrandingStore } from '@/stores/brandingStore';

afterEach(() => {
  cleanup();
  resetBrandingStore();
});

function setSupportEmail(supportEmail: string) {
  useBrandingStore.getState().setConfig(makeBranding({ supportEmail }));
}

describe('SupportEmailLink', () => {
  it('renders a properly encoded mailto link for a valid address', () => {
    setSupportEmail('help+ops@example.test');

    render(<SupportEmailLink />);

    const link = screen.getByTestId('support-email-link');
    expect(link).toHaveAttribute('href', 'mailto:help%2Bops@example.test');
    expect(link).toHaveTextContent('help+ops@example.test');
  });

  it.each([
    'javascript:alert(1)',
    'help@example.test?body=leaked',
    'help@example.test, attacker@evil.example',
    '"><img src=x onerror=alert(1)>@example.test',
    'not-an-email',
  ])('does not render a link for the unsafe value %s', (supportEmail) => {
    setSupportEmail(supportEmail);

    const { container } = render(<SupportEmailLink />);

    expect(screen.queryByTestId('support-email-link')).toBeNull();
    expect(container.querySelector('a')).toBeNull();

    // Falls back to inert text; the raw value is never an href/src.
    expect(screen.getByTestId('support-email-text')).toHaveTextContent(supportEmail);
    for (const element of Array.from(container.querySelectorAll('*'))) {
      for (const attribute of Array.from(element.attributes)) {
        expect(attribute.value).not.toContain('javascript:');
        expect(attribute.name).not.toBe('href');
        expect(attribute.name).not.toBe('src');
      }
    }
  });

  it('renders nothing when the branding config has no support email', () => {
    setSupportEmail('   ');
    const { container } = render(<SupportEmailLink />);
    expect(container).toBeEmptyDOMElement();
  });
});
