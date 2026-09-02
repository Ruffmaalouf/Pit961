import { BrandMark } from '@/components/brand-mark';
import { SupportEmailLink } from '@/components/support-email-link';
import { Card, CardContent } from '@/components/ui/card';
import { LoginForm } from '@/features/auth/LoginForm';
import { useBrandingStore } from '@/stores/brandingStore';

/**
 * Garage-tenant sign-in screen — the only fully built screen in WP-8.
 *
 * The brand mark and the product display name both come from the runtime
 * branding config. Nothing brand-related is hardcoded here.
 */
export function LoginPage() {
  const productDisplayName = useBrandingStore(
    (state) => state.config?.productDisplayName ?? '',
  );

  return (
    <div className="w-full max-w-[390px]">
      <Card>
        <CardContent className="p-8">
          <div className="mb-6 flex flex-col items-center gap-3">
            <BrandMark size={44} radius={13} fontSize={18} />
            {productDisplayName ? (
              <span
                data-testid="product-display-name"
                className="text-center font-sans text-[15.5px] font-semibold text-text-primary"
              >
                {productDisplayName}
              </span>
            ) : null}
          </div>

          <h1 className="mb-5 font-sans text-[20px] font-semibold text-text-primary">Sign in</h1>

          <LoginForm />
        </CardContent>
      </Card>

      <p className="mt-4 text-center font-sans text-[11.5px] text-text-muted-3">
        <SupportEmailLink />
      </p>
    </div>
  );
}
