using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public interface IRemoteConfig
{
    int GetInt(Parameter parameter);
    string GetString(Parameter parameter);
    bool GetBool(Parameter parameter);
    float GetFloat(Parameter parameter);

    T GetClass<T>(Parameter parameter);

    UniTask FetchDataAsync();

    Action OnRemoteDataFetched { get; set; }

    public bool IsRemoteDataFetched { get; }
}
