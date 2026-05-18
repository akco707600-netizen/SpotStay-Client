using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace 코빚노_프로젝트
{
    public partial class StayListWindow : Window
    {
        private TourSpotItem _tour;
        private List<StayItem> _stays;
        private StayItem _selectedStay;

        private bool _isMapReady = false;

        public StayListWindow(TourSpotItem tour, List<StayItem> stays)
        {
            InitializeComponent();

            _tour = tour;
            _stays = stays ?? new List<StayItem>();

            InitHeader();
            InitList();

            Loaded += StayListWindow_Loaded;
        }

        private async void StayListWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadStayMapAsync();

            if (_stays.Count > 0)
            {
                StayFullList.SelectedIndex = 0;
                UpdatePopup(_stays[0]);
                await FocusStayOnMapAsync(0);
            }
        }

        private void InitHeader()
        {
            if (_tour == null)
                return;

            TxtOriginBadge.Text = _tour.Icon + " " + _tour.Name;
            TxtStayCount.Text = _stays.Count.ToString();
            TxtStaySub.Text = _tour.Name + " 기준";
        }

        private void InitList()
        {
            StayFullList.ItemsSource = _stays;
        }

        private async void StayFullList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            StayItem stay = StayFullList.SelectedItem as StayItem;

            if (stay == null)
                return;

            UpdatePopup(stay);

            int index = StayFullList.SelectedIndex;
            await FocusStayOnMapAsync(index);
        }

        private void UpdatePopup(StayItem stay)
        {
            _selectedStay = stay;
        }

        private async Task LoadStayMapAsync()
        {
            if (_tour == null)
                return;

            if (_tour.MapX == 0 || _tour.MapY == 0)
                return;

            try
            {
                await StayMapWebView.EnsureCoreWebView2Async();

                string html = BuildStayMapHtml();

                StayMapWebView.NavigateToString(html);

                // HTML 안의 focusStay 함수가 생길 때까지 잠깐 기다림
                for (int i = 0; i < 30; i++)
                {
                    string ready = await StayMapWebView.ExecuteScriptAsync("typeof window.focusStay === 'function'");

                    if (ready.Contains("true"))
                    {
                        _isMapReady = true;
                        break;
                    }

                    await Task.Delay(200);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "숙소 지도 오류");
            }
        }

        private string BuildStayMapHtml()
        {
            string kakaoKey = "54dc707b933a254382b930502bb97ddf";

            string tourName = EscapeJs(_tour.Name);
            string tourAddress = EscapeJs(_tour.Address);

            string tourMapX = _tour.MapX.ToString(CultureInfo.InvariantCulture);
            string tourMapY = _tour.MapY.ToString(CultureInfo.InvariantCulture);

            StringBuilder stayArray = new StringBuilder();

            for (int i = 0; i < _stays.Count; i++)
            {
                StayItem s = _stays[i];

                if (s.MapX == 0 || s.MapY == 0)
                    continue;

                if (stayArray.Length > 0)
                    stayArray.Append(",");

                stayArray.Append($@"
{{
    index: {i},
    name: '{EscapeJs(s.Name)}',
    address: '{EscapeJs(s.Address)}',
    distance: '{EscapeJs(s.Distance)}',
    price: '{EscapeJs(s.Price)}',
    mapX: {s.MapX.ToString(CultureInfo.InvariantCulture)},
    mapY: {s.MapY.ToString(CultureInfo.InvariantCulture)}
}}");
            }

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        html, body, #map {{
            width: 100%;
            height: 100%;
            margin: 0;
            padding: 0;
            overflow: hidden;
            font-family: 'Malgun Gothic', sans-serif;
        }}

        .tourInfo {{
            padding: 7px 12px;
            background: white;
            border: 2px solid #FFCC00;
            border-radius: 10px;
            font-size: 13px;
            font-weight: bold;
            color: #222;
            box-shadow: 0 2px 8px rgba(0,0,0,0.18);
        }}

      .stayInfo {{
            width: 340px;
            padding: 14px 16px;
            background: white;
            border-radius: 12px;
            border: 1px solid #ddd;
            font-size: 12px;
            box-shadow: 0 3px 12px rgba(0,0,0,0.18);
            box-sizing: border-box;
        }}

        .stayName {{
            font-size: 14px;
            font-weight: bold;
            color: #222;
            margin-bottom: 7px;
            white-space: normal;
            word-break: keep-all;
        }}

        .staySub {{
            color: #777;
            margin-bottom: 5px;
            line-height: 1.45;
            white-space: normal;
            word-break: keep-all;
        }}

        .stayPrice {{
            margin-top: 8px;
            font-size: 15px;
            font-weight: bold;
            color: #1A73E8;
        }}

        .mapBadge {{
            position: absolute;
            top: 12px;
            right: 12px;
            z-index: 9999;
            background: white;
            border-radius: 12px;
            padding: 9px 14px;
            font-size: 12px;
            font-weight: bold;
            color: #555;
            box-shadow: 0 2px 10px rgba(0,0,0,0.15);
        }}

        .mapBadge strong {{
            color: #FFCC00;
        }}
    </style>

    <script src='https://dapi.kakao.com/v2/maps/sdk.js?appkey={kakaoKey}&autoload=false'></script>
</head>
<body>
    <div id='map'></div>
    <div class='mapBadge'>반경 <strong>20km</strong> · 숙소 <strong>{_stays.Count}</strong>개</div>

    <script>
        var tour = {{
            name: '{tourName}',
            address: '{tourAddress}',
            mapX: {tourMapX},
            mapY: {tourMapY}
        }};

        var stays = [{stayArray}];

        var map;
        var tourMarker;
        var stayMarkers = [];
        var stayInfos = [];

        kakao.maps.load(function() {{
            var mapContainer = document.getElementById('map');
            var tourPosition = new kakao.maps.LatLng(tour.mapY, tour.mapX);

            var mapOption = {{
                center: tourPosition,
                level: 7
            }};

            map = new kakao.maps.Map(mapContainer, mapOption);

            var bounds = new kakao.maps.LatLngBounds();

            // 관광지 기준 마커
            tourMarker = new kakao.maps.Marker({{
                map: map,
                position: tourPosition,
                title: tour.name
            }});

            var tourInfo = new kakao.maps.InfoWindow({{
                content: '<div class=""tourInfo"">📍 ' + tour.name + '</div>'
            }});

            tourInfo.open(map, tourMarker);
            bounds.extend(tourPosition);

            // 반경 원
            var circle = new kakao.maps.Circle({{
                center: tourPosition,
                radius: 20000,
                strokeWeight: 2,
                strokeColor: '#83C67A',
                strokeOpacity: 0.85,
                fillColor: '#BFE3B9',
                fillOpacity: 0.28
            }});

            circle.setMap(map);

            function closeAllStayInfo() {{
                for (var i = 0; i < stayInfos.length; i++) {{
                    if (stayInfos[i]) {{
                        stayInfos[i].close();
                    }}
                }}
            }}

            // 숙소 마커들
            stays.forEach(function(s) {{
                var pos = new kakao.maps.LatLng(s.mapY, s.mapX);
                bounds.extend(pos);

                var marker = new kakao.maps.Marker({{
                    map: map,
                    position: pos,
                    title: s.name
                }});

                var info = new kakao.maps.InfoWindow({{
                    content:
        '<div class=""stayInfo"">' +
            '<div class=""stayName"">🏨 ' + s.name + '</div>' +
            '<div class=""staySub"">📍 ' + s.address + '</div>' +
            '<div class=""staySub"">🚗 관광지에서 ' + s.distance + '</div>' +
            '<div class=""stayPrice"">💰 ' + s.price + '</div>' +
        '</div>'
                }});

                kakao.maps.event.addListener(marker, 'click', function() {{
                    closeAllStayInfo();
                    info.open(map, marker);
                }});

                stayMarkers[s.index] = marker;
                stayInfos[s.index] = info;
            }});

            if (!bounds.isEmpty()) {{
                map.setBounds(bounds);
            }}

            window.focusStay = function(index) {{
                var marker = stayMarkers[index];
                var info = stayInfos[index];

                if (!marker || !info) {{
                    return;
                }}

                closeAllStayInfo();

                map.panTo(marker.getPosition());
                info.open(map, marker);
            }};
        }});
    </script>
</body>
</html>";

            return html;
        }

        private async Task FocusStayOnMapAsync(int index)
        {
            if (!_isMapReady)
                return;

            try
            {
                await StayMapWebView.ExecuteScriptAsync("window.focusStay(" + index + ");");
            }
            catch
            {
            }
        }

        private string EscapeJs(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("\r", "")
                .Replace("\n", " ");
        }

        private void BtnBookStay_Click(object sender, RoutedEventArgs e)
        {
            StayItem stay = null;

            FrameworkElement fe = sender as FrameworkElement;

            if (fe != null)
                stay = fe.DataContext as StayItem;

            if (stay == null)
                stay = _selectedStay;

            if (stay == null)
            {
                MessageBox.Show("숙소를 먼저 선택하세요.");
                return;
            }

            string url = stay.BookingUrl;

            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://search.naver.com/search.naver?query="
                      + Uri.EscapeDataString(stay.Name + " 예약");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}