'use client'
import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { register } from '@/lib/api'
import { saveToken } from '@/lib/auth'
import Link from 'next/link'
export default function RegisterPage() {
  const router = useRouter()
  const [form, setForm] = useState({ email: '', password: '', fullName: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault(); setLoading(true); setError('')
    try { const res = await register(form.email, form.password, form.fullName); saveToken(res.data.token); router.push('/dashboard') }
    catch (err: any) { setError(err.response?.data?.error || 'Registration failed.') }
    finally { setLoading(false) }
  }
  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50">
      <div className="bg-white rounded-2xl shadow-md p-8 w-full max-w-sm">
        <h1 className="text-2xl font-bold text-slate-800 mb-6">Create Account</h1>
        <form onSubmit={handleSubmit} className="space-y-4">
          <input type="text" placeholder="Full Name (optional)" value={form.fullName} onChange={e=>setForm({...form,fullName:e.target.value})} className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <input type="email" placeholder="Email" required value={form.email} onChange={e=>setForm({...form,email:e.target.value})} className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <input type="password" placeholder="Password (min 8, 1 upper, 1 number)" required value={form.password} onChange={e=>setForm({...form,password:e.target.value})} className="w-full border border-slate-200 rounded-lg px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          {error && <p className="text-red-500 text-sm">{error}</p>}
          <button type="submit" disabled={loading} className="w-full bg-brand-600 text-white rounded-lg py-2 font-semibold hover:bg-brand-700 transition disabled:opacity-50">{loading?'Creating...':'Get Started Free'}</button>
        </form>
        <p className="text-sm text-slate-500 mt-4 text-center">Have an account? <Link href="/login" className="text-brand-600 hover:underline">Sign in</Link></p>
      </div>
    </div>
  )
}
