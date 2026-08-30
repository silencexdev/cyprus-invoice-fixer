import Link from 'next/link'
export default function Home() {
  return (
    <main className="min-h-screen flex flex-col items-center justify-center px-4 bg-gradient-to-br from-blue-50 to-white">
      <div className="max-w-2xl w-full text-center space-y-6">
        <h1 className="text-5xl font-bold text-brand-700 tracking-tight">Cyprus Invoice Fixer</h1>
        <p className="text-xl text-slate-600">Paste or upload any invoice. AI extracts, validates against Cyprus VAT rules, and exports a compliant PDF.</p>
        <div className="flex gap-4 justify-center">
          <Link href="/register" className="px-6 py-3 bg-brand-600 text-white rounded-lg font-semibold hover:bg-brand-700 transition">Get Started Free</Link>
          <Link href="/login" className="px-6 py-3 border border-brand-600 text-brand-600 rounded-lg font-semibold hover:bg-brand-50 transition">Sign In</Link>
        </div>
        <div className="grid grid-cols-3 gap-4 pt-8 text-left">
          {[{icon:'🔍',title:'AI Extraction',desc:'Pulls every field from any invoice text or image.'},{icon:'✅',title:'VAT Compliance',desc:'Checks all 14 mandatory Cyprus VAT invoice fields.'},{icon:'📄',title:'PDF Export',desc:'Download a clean, compliant invoice PDF instantly.'}].map((f) => (
            <div key={f.title} className="bg-white rounded-xl p-5 shadow-sm border border-slate-100"><div className="text-3xl mb-2">{f.icon}</div><h3 className="font-semibold text-slate-800">{f.title}</h3><p className="text-sm text-slate-500 mt-1">{f.desc}</p></div>
          ))}
        </div>
      </div>
    </main>
  )
}
