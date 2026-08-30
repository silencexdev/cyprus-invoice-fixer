'use client'
import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { login } from '@/lib/api'
import { saveToken } from '@/lib/auth'
import Link from 'next/link'
export default function LoginPage() {
  const router = useRouter()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try { const res = await login(email, password); saveToken(res.data.token); router.push('/dashboard') }
    catch (err: any) { setError(err.response?.data?.error || 'Login failed.') }
    finally { setLoading(false) }
  }
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <div className="bg-white rounded-2xl shadow-md p-8 w-full max-w-sm">
        <h1 className="text-2xl font-bold text-slate-800 mb-6">Sign In</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <input type="email" placeholder="Email" value={email} required onChange={e=>setEmail(e.target.value)} className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <input type="password" placeholder="Password" value={password} required onChange={e=>setPassword(e.target.value)} className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          {error && <p className="text-red-500 text-sm">{error}</p>}
          <button type="submit" disabled={loading} className="w-full bg-brand-600 text-white rounded-lg py-2 font-semibold hover:bg-brand-700 transition disabled:opacity-50">{loading?'Signing in...':'Sign In'}</button>
        </form>
        <p className="text-sm text-slate-500 mt-4 text-center">No account? <Link href="/register" className="text-brand-600 hover:underline">Register</Link></p>
      </div>
    </div>
  )
}
