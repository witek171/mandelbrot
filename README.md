# Wizualizacja zbioru Mandelbrota w C# Windows Forms
 
Aplikacja umożliwia eksplorację fraktala z zoomem, wybór silnika obliczeń (CPU/GPU) oraz wielu palet kolorów.

## Nawigacja i widok
- **Zoom** – powiększanie i pomniejszanie widoku (kliknięcie lub zaznaczenie obszaru)
- **Przesuwanie** – przeciąganie widoku myszą
- **Historia** – przycisk „Wstecz” do cofnięcia ostatniego zoomu
- **Reset** – powrót do domyślnego widoku

## Silniki obliczeń
- **GPU (OpenCL)** – obliczenia na karcie graficznej (wymaga OpenCL)
- **CPU Parallel** – wielowątkowe obliczenia na procesorze
- **CPU Single** – obliczenia jednowątkowe (referencyjne)

## Inne
- Regulacja **maksymalnej liczby iteracji** – wpływa na szczegółowość i czas renderowania
- **Auto-iteracje** – automatyczne zwiększanie iteracji przy głębokim zoomie
- Dla CPU Parallel: wybór **liczby wątków** (domyślnie wszystkie rdzenie)
- **Cache iteracji** – szybsze ponowne renderowanie przy zmianie tylko palety
- Wyświetlanie **czasu renderowania** i informacji o precyzji/zoom

## Uruchomienie testów wydajnościowych

```bash
dotnet run --project Mandelbrot.ConsoleTest
```

Program uruchamia benchmarki (m.in. skalowalność względem liczby wątków) i zapisuje wyniki do katalogu `MandelbrotTestResults` w formacie CSV.

## Zależności (Mandelbrot.Core)
- **Cloo** / **OpenCL.Net** – OpenCL dla .NET (silnik GPU)
- **ComputeSharp** – GPU compute dla .NET
- **ILGPU** / **ILGPU.Algorithms** – akceleracja GPU
- **System.Drawing.Common** – operacje na bitmapach

