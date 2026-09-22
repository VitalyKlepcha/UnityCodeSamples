using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRemoteConfig
{
    int GetInt(Parameter parameter);
    string GetString(Parameter parameter);
    bool GetBool(Parameter parameter);
    float GetFloat(Parameter parameter);

    T GetClass<T>(Parameter parameter);

    Task FetchDataAsync();

    Action OnRemoteDataFetched { get; set; }

    public bool IsRemoteDataFetched { get; }
}
