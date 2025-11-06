import { useState, useEffect } from "react";
import axios from "axios";

// const BASE_URL = "https://redemptionfe.azurewebsites.net/api/";
const BASE_URL = "https://cccredemption-czeqeud7e5hqf2cc.canadacentral-01.azurewebsites.net/";

interface UseAxiosOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE" | "PATCH";
  body?: any;
  token?: string | null;
  trigger?: boolean;
  headers?: Record<string, string>;
}

export function useAxios(url: string, options:UseAxiosOptions = {}) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const fetchData = async () => {
    setLoading(true);
    try {
      const response = await axios({
        url: `${BASE_URL}${url}`,
        ...options
      });
      setData(response.data);
      setError(null);
    } catch (err:any) {
      setError(err.response?.data?.message || err.message);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [url]); // Re-fetch when URL changes

  return { data, loading, error };
}
