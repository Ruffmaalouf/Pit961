import { AppRoutes } from '@/app/routes';
import { useAppBootstrap } from '@/hooks/useAppBootstrap';
import { useBrandedDocumentTitle } from '@/hooks/useDocumentTitle';

export function App() {
  useAppBootstrap();
  useBrandedDocumentTitle();

  return <AppRoutes />;
}

export default App;
