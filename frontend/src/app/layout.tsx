import type { Metadata } from 'next'
import { Inter } from 'next/font/google'
import './globals.css'
import Providers from '@/components/Providers'
const inter = Inter({ subsets: ['latin'] })
export const metadata: Metadata = { title: 'Cyprus Invoice Fixer', description: 'AI-powered Cyprus VAT invoice checker and PDF exporter' }
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return <html lang="en"><body className={inter.className}><Providers>{children}</Providers></body></html>
}
