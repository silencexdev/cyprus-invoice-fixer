'use client'
import { useEffect, useState } from 'react'
import { useRouter } from 'next/navigation'
import { listInvoices, parseText, parseImage, downloadPdf, deleteInvoice, getMe, createCheckout } from '@/lib/api'
import { isLoggedIn, removeToken } from '@/lib/auth'
export default function DashboardPage() {
  const router = useRouter()
  const [invoices, setInvoices] = useState<any[]>([])
  const [user, setUser] = useState<any>(null)
  const [tab, setTab] = useState<'text'|'image'>('text')
  const [text, setText] = useState('')
  const [file, setFile] = useState<File|null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  useEffect(() => { if (!isLoggedIn()) { router.push('/login'); return }; loadData() }, [])
  const loadData = async () => { const [inv,me] = await Promise.all([listInvoices(),getMe()]); setInvoices(inv.data); setUser(me.data) }
  const handleParse = async () => {
    setLoading(true); setError('')
    try { if (tab==='text') await parseText(text); else if (file) await parseImage(file); setText(''); setFile(null); await loadData() }
    catch (err: any) { setError(err.response?.data?.error||'Failed to parse.') }
    finally { setLoading(false) }
  }
  const handleDownload = async (id: string, num: string) => {
    const res = await downloadPdf(id)
    const url = URL.createObjectURL(new Blob([res.data]))
    const a = document.createElement('a'); a.href=url; a.download=`invoice-${num||id.slice(0,8)}.pdf`; a.click(); URL.revokeObjectURL(url)
  }
  const handleDelete = async (id: string) => { if (!confirm('Delete?')) return; await deleteInvoice(id); await loadData() }
  const handleUpgrade = async () => { const res = await createCheckout(`${window.location.origin}/dashboard?success=1`,`${window.location.origin}/dashboard?cancel=1`); window.location.href=res.data.url }
  return (
    <div className="min-h-screen bg-slate-50">
      <nav className="bg-white border-b border-slate-200 px-6 py-4 flex justify-between items-center">
        <h1 className="text-xl font-bold text-brand-700">Cyprus Invoice Fixer</h1>
        <div className="flex items-center gap-4">
          {user && <span className="text-sm text-slate-500">{user.email} · <span className={`font-semibold ${user.plan==='Paid'?'text-green-600':'text-slate-600'}`}>{user.plan}</span>{user.plan==='Free'&&` (${user.monthlyUsageCount}/3)`}</span>}
          <button onClick={()=>{removeToken();router.push('/')}} className="text-sm text-slate-400 hover:text-red-500">Logout</button>
        </div>
      </nav>
      <div className="max-w-4xl mx-auto px-4 py-8 space-y-8">
        <div className="bg-white rounded-2xl shadow-sm border border-slate-100 p-6">
          <h2 className="text-lg font-semibold text-slate-800 mb-4">Parse Invoice</h2>
          <div className="flex gap-2 mb-4">
            <button onClick={()=>setTab('text')} className={`px-4 py-2 rounded-lg text-sm font-medium ${tab==='text'?'bg-brand-600 text-white':'bg-slate-100 text-slate-600'}`}>Paste Text</button>
            <button onClick={()=>setTab('image')} className={`px-4 py-2 rounded-lg text-sm font-medium ${tab==='image'?'bg-brand-600 text-white':'bg-slate-100 text-slate-600'}`}>Upload Image</button>
          </div>
          {tab==='text'
            ?<textarea value={text} onChange={e=>setText(e.target.value)} placeholder="Paste invoice text..." rows={8} className="w-full border border-slate-200 rounded-lg px-4 py-3 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-brand-500 resize-none" />
            :<input type="file" accept="image/*,.pdf" onChange={e=>setFile(e.target.files?.[0]||null)} className="w-full border border-slate-200 rounded-lg px-4 py-3 text-sm" />
          }
          {error && <p className="text-red-500 text-sm mt-2">{error}</p>}
          <button onClick={handleParse} disabled={loading||(tab==='text'?!text.trim():!file)} className="mt-4 px-6 py-2 bg-brand-600 text-white rounded-lg font-semibold hover:bg-brand-700 transition disabled:opacity-50">{loading?'Processing...':'✨ Parse & Validate'}</button>
        </div>
        <div className="space-y-4">
          <h2 className="text-lg font-semibold text-slate-800">Your Invoices</h2>
          {invoices.length===0&&<p className="text-slate-400 text-sm">No invoices yet.</p>}
          {invoices.map((inv:any)=>(
            <div key={inv.id} className="bg-white rounded-xl border border-slate-100 shadow-sm p-5 flex justify-between items-start gap-4">
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 flex-wrap">
                  <span className="font-semibold text-slate-800">{inv.invoiceNumber||'No number'}</span>
                  <span className={`text-xs px-2 py-0.5 rounded-full font-medium ${inv.status==='Valid'?'bg-green-100 text-green-700':inv.status==='Invalid'?'bg-red-100 text-red-700':'bg-slate-100 text-slate-500'}`}>{inv.status}</span>
                </div>
                <p className="text-sm text-slate-500 mt-1">{inv.supplierName||'—'} → {inv.customerName||'—'} · {inv.currency} {Number(inv.total).toFixed(2)}</p>
                {inv.validationIssues?.length>0&&<ul className="mt-2 space-y-1">{inv.validationIssues.map((i:any)=><li key={i.id} className={`text-xs ${i.severity==='Error'?'text-red-600':'text-yellow-600'}`}>⚠ {i.field}: {i.message}</li>)}</ul>}
              </div>
              <div className="flex gap-2 shrink-0">
                <button onClick={()=>handleDownload(inv.id,inv.invoiceNumber)} className="px-3 py-1.5 text-xs bg-brand-50 text-brand-700 rounded-lg hover:bg-brand-100 font-medium">PDF</button>
                <button onClick={()=>handleDelete(inv.id)} className="px-3 py-1.5 text-xs bg-red-50 text-red-600 rounded-lg hover:bg-red-100 font-medium">Delete</button>
              </div>
            </div>
          ))}
        </div>
        {user?.plan==='Free'&&(
          <div className="bg-brand-50 border border-brand-100 rounded-2xl p-6 flex justify-between items-center">
            <div><h3 className="font-semibold text-brand-700">Upgrade to Paid</h3><p className="text-sm text-slate-600 mt-1">Unlimited invoices, batch import, and more.</p></div>
            <button onClick={handleUpgrade} className="px-5 py-2 bg-brand-600 text-white rounded-lg font-semibold hover:bg-brand-700 text-sm">Upgrade Now</button>
          </div>
        )}
      </div>
    </div>
  )
}
