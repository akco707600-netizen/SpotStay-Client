using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace 코빚노_프로젝트
{
    public class TourApiService
    {
        private const string BaseUrl = "http://apis.data.go.kr/B551011/KorService2";

        // 공공데이터포털에서 받은 한국관광공사 국문 관광정보 서비스_GW serviceKey 넣기
        // 카카오 JavaScript 키 아님.
        private const string ServiceKey = "d59365a75adc8e0ca7cbb65a3fa8265e0130dd6bfbc461f1888829f720006c91";

        public async Task<List<TourSpotItem>> SearchTourSpotsAsync(string keyword, int numOfRows = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                keyword = "제주";

            string url =
                BaseUrl + "/searchKeyword2" +
                "?serviceKey=" + Uri.EscapeDataString(ServiceKey) +
                "&MobileOS=ETC" +
                "&MobileApp=KoreaTravel" +
                "&_type=xml" +
                "&numOfRows=" + numOfRows +
                "&pageNo=1" +
                "&contentTypeId=12" +
                "&keyword=" + Uri.EscapeDataString(keyword);

            using (HttpClient client = new HttpClient())
            {
                string xml = await client.GetStringAsync(url);

                if (xml.Contains("SERVICE_KEY_IS_NOT_REGISTERED_ERROR") ||
                    xml.Contains("SERVICE ERROR") ||
                    xml.Contains("OpenAPI_ServiceResponse"))
                {
                    throw new Exception("관광공사 API 호출 실패\n\n" + xml);
                }

                XDocument doc = XDocument.Parse(xml);

                List<XElement> items = doc.Descendants("item").ToList();

                List<TourSpotItem> result = new List<TourSpotItem>();

                int no = 1;

                foreach (XElement item in items)
                {
                    string title = GetValue(item, "title");
                    string addr1 = GetValue(item, "addr1");
                    string contentId = GetValue(item, "contentid");
                    string contentTypeId = GetValue(item, "contenttypeid");
                    string firstImage = GetValue(item, "firstimage");

                    double mapX = ToDouble(GetValue(item, "mapx"));
                    double mapY = ToDouble(GetValue(item, "mapy"));

                    // 좌표 없는 데이터는 지도에서 못 쓰니까 일단 제외
                    if (mapX == 0 || mapY == 0)
                        continue;

                    string category = GuessCategory(title, addr1);
                    string icon = GuessIcon(category, title);

                    result.Add(new TourSpotItem
                    {
                        No = no++,
                        ContentId = contentId,
                        ContentTypeId = contentTypeId,

                        Icon = icon,
                        Name = title,
                        Address = addr1,

                        Category = category,
                        SubCategory = "관광",
                        Rating = "★ 4." + (no % 9),

                        Region = GuessRegion(addr1),
                        ImageUrl = firstImage,

                        MapX = mapX,
                        MapY = mapY
                    });
                }

                return result;
            }
        }

        public async Task<List<StayItem>> GetNearbyStaysAsync(double mapX, double mapY, int radius = 20000, int numOfRows = 50)
        {
            string url =
                BaseUrl + "/locationBasedList2" +
                "?serviceKey=" + Uri.EscapeDataString(ServiceKey) +
                "&MobileOS=ETC" +
                "&MobileApp=KoreaTravel" +
                "&_type=xml" +
                "&numOfRows=" + numOfRows +
                "&pageNo=1" +
                "&arrange=E" +
                "&contentTypeId=32" +
                "&mapX=" + mapX.ToString(CultureInfo.InvariantCulture) +
                "&mapY=" + mapY.ToString(CultureInfo.InvariantCulture) +
                "&radius=" + radius;

            using (HttpClient client = new HttpClient())
            {
                string xml = await client.GetStringAsync(url);

                if (xml.Contains("SERVICE ERROR") ||
                    xml.Contains("OpenAPI_ServiceResponse") ||
                    xml.Contains("SERVICE_KEY_IS_NOT_REGISTERED_ERROR"))
                {
                    throw new Exception("주변 숙소 API 호출 실패\n\n" + xml);
                }

                XDocument doc = XDocument.Parse(xml);
                List<XElement> items = doc.Descendants("item").ToList();

                List<StayItem> result = new List<StayItem>();
                int no = 1;

                foreach (XElement item in items)
                {
                    string title = GetValue(item, "title");
                    string addr1 = GetValue(item, "addr1");
                    string contentId = GetValue(item, "contentid");
                    string contentTypeId = GetValue(item, "contenttypeid");
                    string firstImage = GetValue(item, "firstimage");

                double stayMapX = ToDouble(GetValue(item, "mapx"));
double stayMapY = ToDouble(GetValue(item, "mapy"));

int apiDist = ToInt(GetValue(item, "dist"));

int dist = apiDist > 0
    ? apiDist
    : CalculateDistanceMeters(mapY, mapX, stayMapY, stayMapX);

string price = MakeEstimatedPrice(title, no, dist);

if (string.IsNullOrWhiteSpace(title))
    continue;

                    result.Add(new StayItem
                    {
                        No = no,
                        ContentId = contentId,
                        ContentTypeId = contentTypeId,

                        Icon = GuessStayIcon(title),
                        Name = title,
                        Address = addr1,
                        HotelType = GuessStayType(title),

                        DistanceMeters = dist,
                        Distance = FormatDistance(dist),

                        Price = price,

                        Rating = "★ 4." + ((no + 4) % 10),
                        ReviewText = "리뷰 " + (80 + no * 37) + "개",

                        Tag1 = MakeTag1(no),
                        Tag2 = MakeTag2(no),
                        Tag3 = MakeTag3(no),

                        BookingUrl = "https://search.naver.com/search.naver?query="
                 + Uri.EscapeDataString(title + " 예약"),

                        ImageUrl = firstImage,

                        MapX = stayMapX,
                        MapY = stayMapY
                    });
                    no++;
                }

                return result.OrderBy(x => x.DistanceMeters).ToList();
            }
        } //주변숙소

        private int ToInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            double temp;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out temp))
                return (int)Math.Round(temp);

            return 0;
        }

        private string FormatDistance(int meters)
        {
            if (meters <= 0)
                return "거리정보 없음";

            if (meters < 1000)
                return meters + "m";

            double km = meters / 1000.0;
            return km.ToString("0.0", CultureInfo.InvariantCulture) + "km";
        }

        private string GuessStayIcon(string title)
        {
            if (title.Contains("펜션")) return "🏕️";
            if (title.Contains("게스트")) return "🏡";
            if (title.Contains("리조트")) return "🏖️";
            if (title.Contains("모텔")) return "🏩";
            if (title.Contains("스테이")) return "🛖";

            return "🏨";
        }

        private string GuessStayType(string title)
        {
            if (title.Contains("펜션")) return "펜션";
            if (title.Contains("게스트")) return "게스트하우스";
            if (title.Contains("리조트")) return "리조트";
            if (title.Contains("모텔")) return "모텔";
            if (title.Contains("스테이")) return "스테이";
            if (title.Contains("호텔")) return "호텔";

            return "숙소";
        }

        private string MakeTag1(int no)
        {
            string[] tags = { "무료주차", "조식 포함", "등산로 근처", "자연경관", "가성비" };
            return tags[(no - 1) % tags.Length];
        }

        private string MakeTag2(int no)
        {
            string[] tags = { "Wi-Fi", "수영장", "바베큐", "픽업가능", "가족여행" };
            return tags[(no - 1) % tags.Length];
        }

        private string MakeTag3(int no)
        {
            string[] tags = { "예약가능", "깔끔함", "감성숙소", "근처맛집", "오션뷰" };
            return tags[(no - 1) % tags.Length];
        }

        private string GetValue(XElement item, string name)
        {
            XElement el = item.Element(name);

            if (el == null)
                return "";

            return el.Value.Trim();
        }

        private double ToDouble(string value)
        {
            double result;

            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
                return result;

            return 0;
        }

        private string GuessRegion(string address)
        {
            if (address.Contains("제주")) return "제주특별자치도";
            if (address.Contains("대전")) return "대전광역시";
            if (address.Contains("서울")) return "서울특별시";
            if (address.Contains("강원")) return "강원도";
            if (address.Contains("경남") || address.Contains("경상남도")) return "경상남도";
            if (address.Contains("전남") || address.Contains("전라남도")) return "전라남도";

            return "기타";
        }

        private string GuessCategory(string title, string address)
        {
            string text = (title + " " + address);

            if (text.Contains("해수욕장") || text.Contains("해변") || text.Contains("바다"))
                return "해변";

            if (text.Contains("국립공원") || text.Contains("산") || text.Contains("오름") || text.Contains("봉"))
                return "국립공원";

            if (text.Contains("궁") || text.Contains("성") || text.Contains("유적") || text.Contains("박물관") || text.Contains("문화"))
                return "문화유적";

            if (text.Contains("시장") || text.Contains("맛집") || text.Contains("먹거리"))
                return "맛집";

            if (text.Contains("숙소") || text.Contains("호텔") || text.Contains("펜션"))
                return "숙소";

            if (text.Contains("숲") || text.Contains("수목원") || text.Contains("휴양림"))
                return "숲길";

            return "관광지";
        }

        private string GuessIcon(string category, string title)
        {
            if (category == "해변") return "🏖️";
            if (category == "국립공원") return "🏔️";
            if (category == "문화유적") return "🏛️";
            if (category == "맛집") return "🍜";
            if (category == "숙소") return "🏨";
            if (category == "숲길") return "🌿";

            if (title.Contains("폭포")) return "🌊";
            if (title.Contains("섬")) return "🏝️";
            if (title.Contains("공원")) return "🌺";

            return "📍";
        }

        private int CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            if (lat1 == 0 || lng1 == 0 || lat2 == 0 || lng2 == 0)
                return 0;

            const double earthRadius = 6371000; // meter

            double dLat = ToRadian(lat2 - lat1);
            double dLng = ToRadian(lng2 - lng1);

            double rLat1 = ToRadian(lat1);
            double rLat2 = ToRadian(lat2);

            double a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return (int)Math.Round(earthRadius * c);
        }

        private double ToRadian(double degree)
        {
            return degree * Math.PI / 180.0;
        }

        private string MakeEstimatedPrice(string title, int no, int distanceMeters)
        {
            int price = 90000;

            if (title.Contains("게스트"))
                price = 45000;
            else if (title.Contains("모텔"))
                price = 60000;
            else if (title.Contains("펜션"))
                price = 120000;
            else if (title.Contains("리조트"))
                price = 180000;
            else if (title.Contains("호텔"))
                price = 110000;
            else if (title.Contains("스테이") || title.Contains("독채"))
                price = 160000;

            price += (no % 5) * 10000;

            if (distanceMeters > 0 && distanceMeters < 2000)
                price += 10000;

            return "예상 ₩" + price.ToString("#,##0") + "/박";
        }
    }
}