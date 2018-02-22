# MethodCallbackReturn Requirements

## Overview

DeviceClient class is used by the device client callback delegate to prepare the callback results. 

## References


##Exposed API
```csharp

public sealed class MethodCallbackReturn
{
    MethodCallbackReturn(int status)

    public static MethodCallbackReturn MethodCallbackReturnFactory(string result, int status)

    public string Result
    public int Status
}
```


### MethodCallbackReturn constructor
```csharp
public MethodCallbackReturn(byte[] result, int status)
```

### Result
```csharp
internal string Result
```

**SRS_METHODCALLBACKRETURN_10_002: [** Result shall check if the input is validate JSON **]**
**SRS_METHODCALLBACKRETURN_10_003: [** Result shall percolate the invalid token exception to the caller **]**
