namespace IonosDns;

/// <summary>
/// Represents the result status of a DNS record operation.
/// </summary>
public enum RecordStatus
{
    /// <summary>Record was successfully created.</summary>
    Created,
    
    /// <summary>Record was successfully updated.</summary>
    Updated,
    
    /// <summary>Record already exists with matching configuration, no changes made.</summary>
    Unchanged,
    
    /// <summary>Record was successfully deleted.</summary>
    Deleted,
    
    /// <summary>Record was already absent, no deletion needed.</summary>
    AlreadyAbsent,
    
    /// <summary>API authentication failed (invalid API key).</summary>
    Unauthorized,
    
    /// <summary>Zone or record was not found.</summary>
    NotFound,
    
    /// <summary>Operation resulted in a conflict.</summary>
    Conflict
}
