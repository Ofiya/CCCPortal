import { useState, useEffect } from "react";
import axios from "axios";

const BASE_URL = "https://cccredemption-czeqeud7e5hqf2cc.canadacentral-01.azurewebsites.net/api/";

interface UseAxiosOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
  body?: any;
  token?: string | null;
  trigger?: boolean;
  headers?: Record<string, string>;
}

export function useAxios(url: string, options: UseAxiosOptions = {}) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const getAuthHeaders = () => {
    const token = options.token || localStorage.getItem("Token");
    const headers: Record<string, string> = options.headers || {};
    
    if (token) {
      headers.Authorization = `Bearer ${token}`;
    }
    
    return headers;
  };

  const fetchData = async () => {
    setLoading(true);
    try {
      const response = await axios({
        url: `${BASE_URL}${url}`,
        method: options.method || "GET",
        data: options.body,
        headers: getAuthHeaders(),
      });
      setData(response.data);
      setError(null);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [url, options.token]);

  return { data, loading, error, refetch: fetchData };
}
