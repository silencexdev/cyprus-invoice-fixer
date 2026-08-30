import axios from 'axios'
const api = axios.create({ baseURL: process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000' })
api.interceptors.request.use((config) => {
  const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})
export default api
export const register = (email: string, password: string, fullName?: string) => api.post('/api/auth/register', { email, password, fullName })
export const login = (email: string, password: string) => api.post('/api/auth/login', { email, password })
export const getMe = () => api.get('/api/me')
export const parseText = (text: string) => api.post('/api/invoice/parse/text', { text })
export const parseImage = (file: File) => { const form = new FormData(); form.append('file', file); return api.post('/api/invoice/parse/image', form, { headers: { 'Content-Type': 'multipart/form-data' } }) }
export const listInvoices = (page = 1, pageSize = 10) => api.get(`/api/invoice?page=${page}&pageSize=${pageSize}`)
export const getInvoice = (id: string) => api.get(`/api/invoice/${id}`)
export const deleteInvoice = (id: string) => api.delete(`/api/invoice/${id}`)
export const downloadPdf = (id: string) => api.get(`/api/invoice/${id}/pdf`, { responseType: 'blob' })
export const validateInvoice = (id: string) => api.get(`/api/invoice/${id}/validate`)
export const createCheckout = (successUrl: string, cancelUrl: string) => api.post('/api/billing/checkout', { successUrl, cancelUrl })
