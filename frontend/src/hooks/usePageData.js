// ============================================================
// FILE   : hooks/usePageData.js
// LAYER  : View support — shared data-loading hook
// PURPOSE: Sixteen pages repeated the same three pieces of state and
//          the same try/catch/finally. This owns that pattern once, so
//          every page loads, errors and refreshes identically.
// USAGE  : const { data, loading, error, reload, setError } =
//              usePageData(() => feedApi.list({ sort }), [sort])
// ============================================================
import { useState, useEffect, useCallback } from 'react'
import { describeError } from '../services/api'

export function usePageData(loader, deps = [], fallbackMessage = 'Could not load this page.') {
  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  const reload = useCallback(async () => {
    setLoading(true)
    try {
      setData(await loader())
      setError('')
    } catch (err) {
      // describeError distinguishes "server is down" from "request refused"
      setError(describeError(err, fallbackMessage))
    } finally {
      setLoading(false)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  useEffect(() => { reload() }, [reload])

  return { data, loading, error, reload, setError, setData }
}

// For pages that fire several requests at once (workspace, network, profile).
export function useAll(loaders, deps = [], fallbackMessage = 'Could not load this page.') {
  return usePageData(() => Promise.all(loaders.map(fn => fn())), deps, fallbackMessage)
}
