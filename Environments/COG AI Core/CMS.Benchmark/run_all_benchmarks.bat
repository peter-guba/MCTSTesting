@echo off
FOR %%f in (Resources\BenchmarkSets\*.xml) do CMS.Benchmark.exe %%~nf
