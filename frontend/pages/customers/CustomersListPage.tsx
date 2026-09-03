import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Spinner } from '@/components/ui/spinner';
import { CustomerFormModal } from '@/features/customers/CustomerFormModal';
import * as customersApi from '@/features/customers/api';
import { useCrumb } from '@/hooks/useCrumb';
import { ApiError } from '@/services/apiClient';
import type { CustomerListItemDto } from '@/types/api';

function initials(item: CustomerListItemDto): string {
  const first = item.firstName.trim()[0] ?? '';
  const last = (item.lastName ?? '').trim()[0] ?? '';
  return (first + last).toUpperCase() || '?';
}

export function CustomersListPage() {
  useCrumb('CUSTOMERS & VEHICLES');
  const navigate = useNavigate();

  const [items, setItems] = useState<CustomerListItemDto[] | null>(null);
  const [totalCount, setTotalCount] = useState(0);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isFormOpen, setIsFormOpen] = useState(false);

  useEffect(() => {
    const controller = new AbortController();
    setError(null);
    // Reset to a loading state on every new search, not just the first load,
    // so a slow query never silently shows stale results as if current.
    setItems(null);

    const timer = window.setTimeout(() => {
      customersApi
        .searchCustomers({ search: search.trim() || undefined, signal: controller.signal })
        .then((response) => {
          setItems(response.items);
          setTotalCount(response.totalCount);
        })
        .catch((err) => {
          if (controller.signal.aborted) return;
          setError(err instanceof ApiError ? err.title : 'Something went wrong. Please try again.');
          setItems([]);
        });
    }, search ? 250 : 0);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [search]);

  function handleCreated(customer: { id: string }) {
    navigate(`/customers/${customer.id}`);
  }

  return (
    <div>
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="font-sans text-[21px] font-semibold tracking-tight text-text-primary">
            Customers &amp; vehicles
          </h1>
          <div className="mt-1 font-mono text-[11.5px] tracking-wide text-text-muted-3">
            {items ? `${totalCount} CUSTOMER${totalCount === 1 ? '' : 'S'} ON FILE` : 'LOADING…'}
          </div>
        </div>
        <Button onClick={() => setIsFormOpen(true)} data-testid="new-customer-button">
          New customer
        </Button>
      </div>

      <div className="mt-4 max-w-[360px]">
        <Input
          placeholder="Search name, phone or email"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          data-testid="customer-search-input"
        />
      </div>

      <div className="mt-4 overflow-hidden rounded-panel border border-border-subtle bg-surface-card">
        {items === null ? (
          <div className="flex items-center justify-center gap-2 p-10 text-text-muted-2" data-testid="customers-loading">
            <Spinner />
            <span className="font-sans text-[13px]">Loading customers…</span>
          </div>
        ) : error ? (
          <div className="p-10 text-center" data-testid="customers-error">
            <p className="font-sans text-[13.5px] text-status-critical">{error}</p>
            <Button variant="outline" size="sm" className="mt-3" onClick={() => setSearch((s) => s)}>
              Try again
            </Button>
          </div>
        ) : items.length === 0 ? (
          <div className="p-10 text-center" data-testid="customers-empty">
            <p className="font-sans text-[13.5px] text-text-muted-1">
              {search ? 'No customers match your search.' : 'No customers yet. Create the first one to get started.'}
            </p>
          </div>
        ) : (
          items.map((item) => (
            <div
              key={item.id}
              onClick={() => navigate(`/customers/${item.id}`)}
              data-testid={`customer-row-${item.id}`}
              className="flex cursor-pointer items-center gap-3.5 border-b border-border-subtle px-[17px] py-[13px] last:border-b-0 hover:bg-surface-card-item"
            >
              <div className="flex h-[34px] w-[34px] flex-none items-center justify-center rounded-full border border-border-subtle bg-surface-card-item font-sans text-[11px] font-semibold text-text-primary">
                {initials(item)}
              </div>
              <div className="min-w-0 flex-1">
                <div className="font-sans text-[13.5px] font-semibold text-text-primary">
                  {item.firstName} {item.lastName ?? ''}
                </div>
                <div className="mt-0.5 font-sans text-[11.5px] text-text-muted-1">
                  {item.phone}
                  {item.email ? ` · ${item.email}` : ''}
                </div>
              </div>
              <div className="flex-none font-mono text-[11px] text-text-muted-2">
                {item.vehicleCount} vehicle{item.vehicleCount === 1 ? '' : 's'}
              </div>
              {item.isFleet ? (
                <span className="flex-none rounded-pill bg-[var(--accent-focus-ring)] px-[7px] py-[3px] font-mono text-[9.5px] font-semibold uppercase tracking-[0.09em] text-accent-primary">
                  Fleet
                </span>
              ) : null}
            </div>
          ))
        )}
      </div>

      <CustomerFormModal open={isFormOpen} onOpenChange={setIsFormOpen} onSaved={handleCreated} />
    </div>
  );
}
