# RadioLinkSim

İki koordinat verince aradaki mesafeyi, arazi yüksekliklerini ve aşağı yukarı gerçek mesafeyi çıkaran ufak bir proje. Frontend de var, API de var. Yükseklik bilgisi için Open Elevation kullanıyor.

## Hesaplamaları nasıl yaptım

### 1. Kuş uçuşu mesafe

İlk olarak iki koordinat arasındaki kuş uçuşu mesafeyi hesaplamaya çalıştım. Bunun için Haversine formülünü kullandım. Bu hesaplama sonucunda başlangıç ve bitiş noktası arasındaki mesafeyi metre olarak buluyorum.

Kodda burası: `CalculateDistanceMeters`

### 2. Anten açısı

Sonra başlangıç noktasından hedef noktasına doğru olan açıyı hesaplamaya çalıştım. Koordinatları kullanarak ilk kerteriz açısını buluyorum. Bu değer antenin hedefe doğru hangi yönde olması gerektiğini gösteriyor.

Kodda burası: `CalculateInitialBearingRadians`

### 3. Ara noktalar

Sadece başlangıç ve bitiş noktasını kullanmak yeterli olmadığı için aradaki noktaları da hesaplamaya çalıştım. Rotayı `stepMeters` değerine göre küçük parçalara böldüm. Her adım için yeni koordinatı hesaplayıp o koordinatın yükseklik bilgisini alıyorum.

Mesela `stepMeters: 100` verilirse yaklaşık her 100 metrede bir nokta oluşturur. Değeri küçültürsek daha detaylı olur ama daha çok nokta ve istek çıkar.

Kodda burası: `CalculateDestinationPoint`

### 4. Efektif mesafe

Son olarak yükseklik değişimlerini de dahil ederek efektif mesafeyi hesaplamaya çalıştım. Her iki ara nokta arasındaki yatay mesafeyi ve yükseklik farkını kullanıyorum. Bu iki değerden eğimli mesafeyi şu şekilde hesaplıyorum:

```text
eğimli mesafe = karekök(yatay mesafe² + yükseklik farkı²)
```

Sonra bütün küçük eğimli mesafeler toplanıyor ve efektif mesafe çıkıyor.

Kodda burası: `CalculateEffectiveDistance`

## Projeyi çalıştırma

### Önerilen yöntem: Docker Compose

En kolay yöntem Docker Compose kullanmak. Bunun için bilgisayarda Docker Desktop kurulu olması yeterli. Proje klasöründe şu komutu çalıştırıyorum:

```bash
docker compose up --build
```

Biraz build alır sonra:

- Frontend: http://localhost:4294
- Backend API: http://localhost:9499
- Scalar API dokümantasyonu: http://localhost:9499/scalar/v1
- OpenAPI JSON: http://localhost:9499/openapi/v1.json

Yani frontend `4294` portunda, backend ve Scalar ise `9499` portunda çalışıyor. Scalar ayrı bir servis olmadığı için backend ile aynı portu kullanıyor.

Arka planda çalışsın istenirse:

```bash
docker compose up -d --build
```

Kapatmak için:

```bash
docker compose down
```

Not: API yükseklikleri `api.open-elevation.com` üzerinden aldığı için internet bağlantısı lazım.

### Visual Studio ile çalıştırma

Docker kullanmak istemezsem backend'i Visual Studio üzerinden de açabilirim. Bunun için genel olarak şunlar lazım:

- Visual Studio 2026 (18.x) veya .NET 10 destekleyen başka bir IDE
- .NET 10 SDK
- Node.js ve npm
- Frontend tarafı için Angular 21

Önce `RadioLinkSim.slnx` dosyasını Visual Studio ile açıp backend'i çalıştırıyorum. Backend `http://localhost:9499` adresinde açılıyor. Visual Studio çalıştırınca Scalar sayfası da otomatik açılabilir.

Frontend için ayrı bir terminal açıp şunları çalıştırıyorum:

```bash
cd frontend
npm install
npm start
```

Frontend `http://localhost:4294` adresinde açılıyor. `/api` istekleri proxy ayarı ile `http://localhost:9499` adresindeki backend'e gidiyor. Bu yüzden frontend'i açmadan önce backend'in çalışıyor olması gerekiyor.

### Terminalden çalıştırma

Visual Studio olmadan iki ayrı terminal kullanarak da çalıştırabilirim.

İlk terminalde backend:

```bash
dotnet restore
dotnet run
```

İkinci terminalde frontend:

```bash
cd frontend
npm install
npm start
```

Burada kullanılan frontend paket sürümleri `package.json` dosyasında bulunuyor. Projede Angular 21 ve npm 11 kullanılıyor.

## Örnek API isteği

Endpoint:

```text
POST http://localhost:9499/api/link-profile
```

İstanbul - Ankara gibi bir deneme isteği:

```bash
curl -X POST http://localhost:9499/api/link-profile \
  -H "Content-Type: application/json" \
  -d '{
    "latA": 41.0082,
    "lonA": 28.9784,
    "latB": 39.9334,
    "lonB": 32.8597,
    "stepMeters": 5000
  }'
```

Alanlar çok karışık değil:

- `latA`, `lonA`: başlangıç koordinatı
- `latB`, `lonB`: bitiş koordinatı
- `stepMeters`: kaç metrede bir yükseklik noktası alınacağı

## Örnek response

Response aşağı yukarı böyle geliyor. Yükseklikler dış servisten geldiği için sayılar değişebilir, ayrıca uzun olmasın diye `points` listesini kısa kestim.

```json
{
  "greatCircleDistanceMeters": 351000.12,
  "effectiveDistanceMeters": 351025.48,
  "stepMeters": 5000,
  "pointCount": 72,
  "points": [
    {
      "latitude": 41.0082,
      "longitude": 28.9784,
      "distanceFromAMeters": 0,
      "elevationMeters": 39
    },
    {
      "latitude": 40.9951,
      "longitude": 29.0358,
      "distanceFromAMeters": 5000,
      "elevationMeters": 82
    }
  ]
}
```

Buradaki `greatCircleDistanceMeters` kuş uçuşu mesafe, `effectiveDistanceMeters` yükseklik farkları eklenmiş mesafe. `points` ise haritada/profil grafiğinde kullanılacak ara noktalar.
