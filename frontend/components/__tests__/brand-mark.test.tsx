import { act, cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { BrandMark } from '@/components/brand-mark';
import { makeBranding } from '@/lib/test-utils';
import { resetBrandingStore, useBrandingStore } from '@/stores/brandingStore';

afterEach(() => {
  // Unmount before touching the store so store resets never fire an update
  // into a still-mounted subscriber (act warning).
  cleanup();
  resetBrandingStore();
});

function setBranding(overrides: Parameters<typeof makeBranding>[0]) {
  useBrandingStore.getState().setConfig(makeBranding(overrides));
}

describe('BrandMark logoUrl safety', () => {
  it('renders a logo image only for an http(s) URL', () => {
    setBranding({ logoUrl: 'https://cdn.example.test/brand/mark.png' });

    render(<BrandMark />);

    const img = screen.getByTestId('brand-mark-logo');
    expect(img).toHaveAttribute('src', 'https://cdn.example.test/brand/mark.png');
  });

  it.each([
    'javascript:alert(1)',
    'JAVASCRIPT:alert(1)',
    'data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==',
    'vbscript:msgbox(1)',
    '//evil.example/logo.png',
  ])('never renders %s as an executable src or href', (unsafeLogoUrl) => {
    setBranding({ productDisplayName: 'Northgate Works', logoUrl: unsafeLogoUrl });

    const { container } = render(<BrandMark />);

    // No image element at all — it falls back to the glyph-only brand mark.
    expect(screen.queryByTestId('brand-mark-logo')).toBeNull();
    expect(container.querySelector('img')).toBeNull();

    // And the hostile value appears in no attribute anywhere in the subtree.
    for (const element of Array.from(container.querySelectorAll('*'))) {
      for (const attribute of Array.from(element.attributes)) {
        expect(attribute.value).not.toContain('javascript:');
        expect(attribute.value.toLowerCase()).not.toContain(unsafeLogoUrl.toLowerCase());
      }
    }

    // The fallback is the initial derived from the runtime display name.
    expect(screen.getByTestId('brand-mark')).toHaveTextContent('N');
  });

  it('derives the mark letter from the runtime display name, not a hardcoded letter', () => {
    setBranding({ productDisplayName: 'Zephyr Motorworks', logoUrl: '' });
    const { rerender } = render(<BrandMark />);
    expect(screen.getByTestId('brand-mark')).toHaveTextContent('Z');

    // Wrapped in act(): the store update re-renders an already-mounted subscriber.
    act(() => {
      setBranding({ productDisplayName: 'Harbour Auto', logoUrl: '' });
    });
    rerender(<BrandMark />);
    expect(screen.getByTestId('brand-mark')).toHaveTextContent('H');
  });

  it('renders a glyph-only mark with no letter when branding is unavailable', () => {
    render(<BrandMark />);
    expect(screen.getByTestId('brand-mark').textContent).toBe('');
    expect(screen.queryByTestId('brand-mark-logo')).toBeNull();
  });
});
