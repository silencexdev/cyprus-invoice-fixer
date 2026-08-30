export const saveToken = (token: string) => localStorage.setItem('token', token)
export const getToken = () => localStorage.getItem('token')
export const removeToken = () => localStorage.removeItem('token')
export const isLoggedIn = () => !!getToken()
