namespace IonosDns;

/// <summary>
/// DNS record types supported by the IONOS API.
/// </summary>
public enum RecordType
{
    /// <summary>IPv4 address record.</summary>
    A,
    
    /// <summary>IPv6 address record.</summary>
    AAAA,
    
    /// <summary>Canonical name (alias) record.</summary>
    CNAME,
    
    /// <summary>Mail exchange record.</summary>
    MX,
    
    /// <summary>Name server record.</summary>
    NS,
    
    /// <summary>Start of authority record.</summary>
    SOA,
    
    /// <summary>Service locator record.</summary>
    SRV,
    
    /// <summary>Text record.</summary>
    TXT,
    
    /// <summary>Certification authority authorization record.</summary>
    CAA,
    
    /// <summary>TLS authentication record.</summary>
    TLSA,
    
    /// <summary>S/MIME certificate association record.</summary>
    SMIMEA,
    
    /// <summary>SSH public key fingerprint record.</summary>
    SSHFP,
    
    /// <summary>Delegation signer record.</summary>
    DS,
    
    /// <summary>HTTPS service binding record.</summary>
    HTTPS,
    
    /// <summary>Service binding record.</summary>
    SVCB,
    
    /// <summary>Certificate record.</summary>
    CERT,
    
    /// <summary>Uniform resource identifier record.</summary>
    URI,
    
    /// <summary>Responsible person record.</summary>
    RP,
    
    /// <summary>Location record.</summary>
    LOC,
    
    /// <summary>OpenPGP public key record.</summary>
    OPENPGPKEY,

	/// <summary>Unknown record.</summary>
	UNKNOWN
}
