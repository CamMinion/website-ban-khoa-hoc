using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Text;

public class VnPayLibrary
{
    // Dùng StringComparer.Ordinal để sắp xếp A-Z chuẩn xác 100%
    private SortedList<string, string> _requestData = new SortedList<string, string>(StringComparer.Ordinal);
    private SortedList<string, string> _responseData = new SortedList<string, string>(StringComparer.Ordinal);

    public void AddRequestData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) _requestData.Add(key, value);
    }

    public void AddResponseData(string key, string value)
    {
        if (!string.IsNullOrEmpty(value)) _responseData.Add(key, value);
    }

    public string GetResponseData(string key)
    {
        return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
    }

    public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
    {
        StringBuilder data = new StringBuilder();
        foreach (KeyValuePair<string, string> kv in _requestData)
        {
            if (data.Length > 0) data.Append("&");
            data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
        }

        string queryString = data.ToString();

        // 💡 THÊM DÒNG NÀY (ĐỂ XEM CHUỖI GỐC ĐANG ĐƯỢC HASH)
        Console.WriteLine("VNPAY HASH STRING: " + queryString);
        // -----------------------------------------------------
        string vnp_SecureHash = HmacSHA512(vnp_HashSecret, queryString);
        string paymentUrl = baseUrl + "?" + queryString + "&vnp_SecureHash=" + vnp_SecureHash;

        return paymentUrl;
    }

    public bool ValidateSignature(string inputHash, string vnp_HashSecret)
    {
        StringBuilder data = new StringBuilder();
        foreach (KeyValuePair<string, string> kv in _responseData)
        {
            if (kv.Key != "vnp_SecureHash" && kv.Key != "vnp_SecureHashType")
            {
                if (data.Length > 0) data.Append("&");
                data.Append(kv.Key + "=" + WebUtility.UrlEncode(kv.Value));
            }
        }

        string myChecksum = HmacSHA512(vnp_HashSecret, data.ToString());
        return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
    }

    private string HmacSHA512(string key, string inputData)
    {
        var hash = new StringBuilder();
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
        using (var hmac = new HMACSHA512(keyBytes))
        {
            byte[] hashValue = hmac.ComputeHash(inputBytes);
            foreach (var theByte in hashValue)
            {
                hash.Append(theByte.ToString("x2"));
            }
        }
        return hash.ToString();
    }
}