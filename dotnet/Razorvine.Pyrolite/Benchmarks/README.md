# Micro Benchmarks

## Run the Performance Tests

1. Navigate to the benchmarks directory (dotnet\Razorvine.Pyrolite\Benchmarks\)

2. Run the benchmarks in Release, choose one of the benchmarks when prompted

```cmd
dotnet run -c Release
```
   
3. To run specific tests only, pass in the filter to the harness:

```cmd
dotnet run -c Release -- --filter namespace*
dotnet run -c Release -- --filter *typeName*
dotnet run -c Release -- --filter *.methodName
dotnet run -c Release -- --filter namespace.typeName.methodName
```

4. To find out more about supported command line arguments run

```cmd
dotnet run -c Release -- --help
```
