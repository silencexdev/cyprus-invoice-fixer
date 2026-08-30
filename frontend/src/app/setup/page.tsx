'use client'
import { useState, useEffect } from 'react'
import { useRouter } from 'next/navigation'
import axios from 'axios'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'

type Step = 'check' | 'database' | 'ai' | 'stripe' | 'done'

export default function SetupPage() {
  const router = useRouter()
  const [step, setStep] = useState<Step>('check')
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [config, setConfig] = useState({
    postgresPassword: '',
    jwtSecret: '',
    aiProvider: 'openai' as 'openai' | 'ollama',
    openAiKey: '',
    ollamaUrl: 'http://ollama:11434',
    ollamaModel: 'llama3',
    stripeSecretKey: '',
    stripeWebhookSecret: '',
    stripePriceId: '',
    frontendUrl: typeof window !== 'undefined' ? window.location.origin : 'http://localhost:3000',
  })

  useEffect(() => {
    axios.get(`${API}/api/setup/status`)
      .then(r => { if (r.data.configured) router.replace('/') })
      .catch(() => {})
  }, [])

  const submit = async () => {
    setLoading(true); setError('')
    try {
      await axios.post(`${API}/api/setup/configure`, config)
      setStep('done')
    } catch (e: any) {
      setError(e.response?.data?.error || 'Setup failed.')
    } finally {
      setLoading(false)
    }
  }

  const set = (k: keyof typeof config, v: string) => setConfig(c => ({ ...c, [k]: v }))

  if (step === 'done') return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-green-50 to-white">
      <div className="text-center space-y-4">
        <div className="text-6xl">✅</div>
        <h1 className="text-3xl font-bold text-green-700">Setup Complete!</h1>
        <p className="text-slate-600">Cyprus Invoice Fixer is ready.</p>
        <button onClick={() => router.push('/')} className="px-6 py-3 bg-brand-600 text-white rounded-lg font-semibold hover:bg-brand-700">Go to App →</button>
      </div>
    </div>
  )

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-white flex items-center justify-center px-4 py-12">
      <div className="max-w-lg w-full space-y-6">
        <div className="text-center">
          <h1 className="text-4xl font-bold text-brand-700">Cyprus Invoice Fixer</h1>
          <p className="text-slate-500 mt-2">First-run setup — enter your secrets below</p>
        </div>

        <div className="bg-white rounded-2xl shadow-md p-8 space-y-6">

          {/* Database */}
          <section>
            <h2 className="font-semibold text-slate-700 mb-3 flex items-center gap-2"><span className="bg-brand-100 text-brand-700 rounded-full w-6 h-6 flex items-center justify-center text-xs font-bold">1</span> Database</h2>
            <label className="text-sm text-slate-500">Postgres Password</label>
            <input type="password" value={config.postgresPassword} onChange={e=>set('postgresPassword',e.target.value)} placeholder="strong_password" className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" />
          </section>

          {/* AI */}
          <section>
            <h2 className="font-semibold text-slate-700 mb-3 flex items-center gap-2"><span className="bg-brand-100 text-brand-700 rounded-full w-6 h-6 flex items-center justify-center text-xs font-bold">2</span> AI Provider</h2>
            <div className="flex gap-3 mb-3">
              {(['openai','ollama'] as const).map(p => (
                <button key={p} onClick={()=>set('aiProvider',p)} className={`flex-1 py-2 rounded-lg text-sm font-medium border transition ${config.aiProvider===p?'bg-brand-600 text-white border-brand-600':'bg-white text-slate-600 border-slate-200 hover:border-brand-300'}`}>
                  {p==='openai'?'☁️ OpenAI':'🦙 Ollama (local)'}
                </button>
              ))}
            </div>
            {config.aiProvider==='openai' ? (
              <><label className="text-sm text-slate-500">OpenAI API Key</label>
              <input type="password" value={config.openAiKey} onChange={e=>set('openAiKey',e.target.value)} placeholder="sk-..." className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></>
            ) : (
              <div className="space-y-3">
                <div><label className="text-sm text-slate-500">Ollama URL</label>
                <input value={config.ollamaUrl} onChange={e=>set('ollamaUrl',e.target.value)} className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></div>
                <div><label className="text-sm text-slate-500">Model</label>
                <input value={config.ollamaModel} onChange={e=>set('ollamaModel',e.target.value)} placeholder="llama3" className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></div>
              </div>
            )}
          </section>

          {/* Stripe */}
          <section>
            <h2 className="font-semibold text-slate-700 mb-3 flex items-center gap-2"><span className="bg-brand-100 text-brand-700 rounded-full w-6 h-6 flex items-center justify-center text-xs font-bold">3</span> Stripe <span className="text-xs text-slate-400 font-normal">(optional)</span></h2>
            <div className="space-y-3">
              <div><label className="text-sm text-slate-500">Secret Key</label>
              <input type="password" value={config.stripeSecretKey} onChange={e=>set('stripeSecretKey',e.target.value)} placeholder="sk_live_... or sk_test_..." className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></div>
              <div><label className="text-sm text-slate-500">Webhook Secret</label>
              <input type="password" value={config.stripeWebhookSecret} onChange={e=>set('stripeWebhookSecret',e.target.value)} placeholder="whsec_..." className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></div>
              <div><label className="text-sm text-slate-500">Price ID</label>
              <input value={config.stripePriceId} onChange={e=>set('stripePriceId',e.target.value)} placeholder="price_..." className="mt-1 w-full border border-slate-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500" /></div>
            </div>
          </section>

          {error && <p className="text-red-500 text-sm">{error}</p>}

          <button onClick={submit} disabled={loading} className="w-full bg-brand-600 text-white rounded-lg py-3 font-semibold hover:bg-brand-700 transition disabled:opacity-50 text-base">
            {loading ? 'Saving & starting...' : '🚀 Complete Setup'}
          </button>

          <p className="text-xs text-slate-400 text-center">Secrets are stored server-side only and never exposed to the browser after this step.</p>
        </div>
      </div>
    </div>
  )
}
