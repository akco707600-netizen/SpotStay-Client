using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace 코빚노_프로젝트
{
    public partial class MainWindow : Window
    {
        private List<TourSpotItem> _tourList;
        private TourSpotItem _selectedTour;
        private readonly TourApiService _tourApiService = new TourApiService();
        private List<StayItem> _nearbyStayList = new List<StayItem>();

        private string _sortMode = "거리순";


        private bool _isMapLoaded = false;
        private bool _isLoggedIn = false;
        private string _loginUserId = "";

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;

            LoadMockStayData();

            LoadKakaoMap();
        }

        private void LoadMockTourData()
        {
            _tourList = new List<TourSpotItem>
            {
                new TourSpotItem
                {
                    No = 1,
                    Icon = "🏔️",
                    Name = "한라산 국립공원",
                    Address = "제주시 1100로 2070-61",
                    Category = "국립공원",
                    SubCategory = "자연",
                    Rating = "★ 4.9",
                    MapX = 126.5312,
                    MapY = 33.3617
                },
                new TourSpotItem
                {
                    No = 2,
                    Icon = "🌋",
                    Name = "성산일출봉",
                    Address = "서귀포시 성산읍 일출로",
                    Category = "유네스코",
                    SubCategory = "자연",
                    Rating = "★ 4.8",
                    MapX = 126.9425,
                    MapY = 33.4580
                },
                new TourSpotItem
                {
                    No = 3,
                    Icon = "🏖️",
                    Name = "협재해수욕장",
                    Address = "제주시 한림읍 협재리",
                    Category = "해변",
                    SubCategory = "자연",
                    Rating = "★ 4.7",
                    MapX = 126.2394,
                    MapY = 33.3932
                },
                new TourSpotItem
                {
                    No = 4,
                    Icon = "🌊",
                    Name = "천지연 폭포",
                    Address = "서귀포시 서귀동 667-7",
                    Category = "자연",
                    SubCategory = "폭포",
                    Rating = "★ 4.6",
                    MapX = 126.5544,
                    MapY = 33.2470
                },
                new TourSpotItem
                {
                    No = 5,
                    Icon = "🌿",
                    Name = "비자림",
                    Address = "제주시 구좌읍 비자숲길",
                    Category = "숲길",
                    SubCategory = "자연",
                    Rating = "★ 4.5",
                    MapX = 126.8113,
                    MapY = 33.4911
                },
                new TourSpotItem
                {
                    No = 6,
                    Icon = "🏝️",
                    Name = "우도",
                    Address = "제주시 우도면",
                    Category = "섬",
                    SubCategory = "자연",
                    Rating = "★ 4.7",
                    MapX = 126.9517,
                    MapY = 33.5044
                },
                new TourSpotItem
                {
                    No = 7,
                    Icon = "🌺",
                    Name = "한림공원",
                    Address = "제주시 한림읍 한림로",
                    Category = "공원",
                    SubCategory = "자연",
                    Rating = "★ 4.4",
                    MapX = 126.2402,
                    MapY = 33.3898
                }
            };

            TourSpotList.ItemsSource = _tourList;
            TxtResultCount.Text = _tourList.Count.ToString();
        }

      

        private void LoadMockStayData()
        {
            List<StayItem> stays = new List<StayItem>
            {
                new StayItem
                {
                    Name = "제주 그랜드 호텔",
                    Distance = "🚗 2.3km",
                    Price = "₩180,000~"
                },
                new StayItem
                {
                    Name = "한라산 게스트하우스",
                    Distance = "🚗 1.1km",
                    Price = "₩45,000~"
                },
                new StayItem
                {
                    Name = "제주 비즈니스",
                    Distance = "🚗 3.8km",
                    Price = "₩95,000~"
                }
            };

            StayList.ItemsSource = stays;
        }

        private void SetSelectedTour(TourSpotItem item)
        {
            _selectedTour = item;

            TxtDetailIcon.Text = item.Icon;
            TxtDetailName.Text = item.Name;
            TxtDetailCategory.Text = item.Category;
            TxtDetailAddress.Text = "📍 " + item.Address;

            MoveMapToTour(item);

            _ = LoadNearbyStayPreviewAsync(item);
        }

        private void SetSelectedCategoryButton(Button selectedButton)
        {
            foreach (var child in CategoryPanel.Children)
            {
                Button btn = child as Button;

                if (btn != null)
                {
                    btn.Style = (Style)FindResource("GrayButton");
                }
            }

            selectedButton.Style = (Style)FindResource("YellowButton");
        }

        private async void CmbRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded)
                return;

            string keyword = GetSelectedRegionKeyword();

            await LoadTourApiDataAsync(keyword);
        }

        private string GetSelectedRegionKeyword()
        {
            ComboBoxItem item = CmbRegion.SelectedItem as ComboBoxItem;

            if (item == null)
                return "제주";

            string region = item.Tag.ToString();

            if (region.Contains("제주")) return "제주";
            if (region.Contains("대전")) return "대전";
            if (region.Contains("서울")) return "서울";
            if (region.Contains("강원")) return "강원";
            if (region.Contains("경상")) return "경상남도";
            if (region.Contains("전라")) return "전라남도";

            return region;
        }

        private void TourSpotCard_Click(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;

            if (fe == null)
                return;

            TourSpotItem item = fe.DataContext as TourSpotItem;

            if (item == null)
                return;

            TourSpotList.SelectedItem = item;
            SetSelectedTour(item);
            LoadMockStayData();
        }

        private void TourSpotList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TourSpotItem item = TourSpotList.SelectedItem as TourSpotItem;

            if (item == null)
                return;

            SetSelectedTour(item);
            LoadMockStayData();
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
                string keyword = TxtSearch.Text.Trim();

                if (string.IsNullOrEmpty(keyword))
                    keyword = "제주";

                await LoadTourApiDataAsync(keyword);
            }

        private void BtnBookStay_Click(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = sender as FrameworkElement;

            if (fe == null)
                return;

            StayItem stay = fe.DataContext as StayItem;

            if (stay == null)
            {
                MessageBox.Show("숙소 정보를 찾을 수 없습니다.");
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

        private async void Keyword_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn == null)
                return;

            SetSelectedCategoryButton(btn);

            string keyword = btn.Content.ToString();

            if (keyword == "전체")
                keyword = GetSelectedRegionKeyword();

            await LoadTourApiDataAsync(keyword);
        }

        private async void BtnNearbyStay_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedTour == null)
            {
                MessageBox.Show("관광지를 먼저 선택하세요.");
                return;
            }

            try
            {
                if (_nearbyStayList == null || _nearbyStayList.Count == 0)
                {
                    _nearbyStayList = await _tourApiService.GetNearbyStaysAsync(
                        mapX: _selectedTour.MapX,
                        mapY: _selectedTour.MapY,
                        radius: 20000,
                        numOfRows: 50);
                }

                StayListWindow window = new StayListWindow(_selectedTour, _nearbyStayList);
                window.Owner = this;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "주변 숙소 전체 조회 오류");
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Owner = this;

                bool? result = loginWindow.ShowDialog();

                if (result == true)
                {
                    // 로그인 버튼은 아예 숨김
                    BtnLogin.Visibility = Visibility.Collapsed;

                    // 사용자 아이디 박스 표시
                    TxtLoginUser.Text = "👤 " + loginWindow.LoginUserId;
                    UserBox.Visibility = Visibility.Visible;

                    // 로그아웃 버튼 표시
                    BtnLogout.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "로그인창 오류");
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.Owner = this;

            bool? result = loginWindow.ShowDialog();

            if (result == true)
            {
                SetLoginUser(loginWindow.LoginUserId);
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private async void LoadKakaoMap()
        {
            try
            {
                await MapWebView.EnsureCoreWebView2Async();

                string kakaoKey = "54dc707b933a254382b930502bb97ddf";

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
        }}
    </style>

    <script src='https://dapi.kakao.com/v2/maps/sdk.js?appkey={kakaoKey}&autoload=false'></script>
</head>
<body>
    <div id='map'></div>

    <script>
        var map;
        var marker;
        var infowindow;

        kakao.maps.load(function() {{
            var container = document.getElementById('map');

            var options = {{
                center: new kakao.maps.LatLng(33.3617, 126.5312),
                level: 8
            }};

            map = new kakao.maps.Map(container, options);

            marker = new kakao.maps.Marker({{
                position: new kakao.maps.LatLng(33.3617, 126.5312)
            }});

            marker.setMap(map);

            infowindow = new kakao.maps.InfoWindow({{
                content: '<div style=""padding:6px 10px;font-size:12px;font-weight:bold;"">한라산 국립공원</div>'
            }});

            infowindow.open(map, marker);

            window.moveMap = function(lat, lng, title) {{
                var position = new kakao.maps.LatLng(lat, lng);

                map.setCenter(position);
                marker.setPosition(position);

                infowindow.setContent(
                    '<div style=""padding:6px 10px;font-size:12px;font-weight:bold;"">' 
                    + title + 
                    '</div>'
                );

                infowindow.open(map, marker);
            }};
        }});
    </script>
</body>
</html>";

                MapWebView.NavigateToString(html);
                _isMapLoaded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "카카오 지도 오류");
            }
        }   //지도

        private async void MoveMapToTour(TourSpotItem item)
        {
            if (item == null)
                return;

            if (!_isMapLoaded)
                return;

            if (item.MapX == 0 || item.MapY == 0)
                return;

            try
            {
                string title = EscapeJavaScriptString(item.Name);

                string script = $"window.moveMap({item.MapY}, {item.MapX}, '{title}');";

                await MapWebView.ExecuteScriptAsync(script);
            }
            catch
            {
                // 지도 로딩 직후 클릭하면 가끔 실패할 수 있어서 일단 무시
            }
        }

        private string EscapeJavaScriptString(string text)
        {
            if (text == null)
                return "";

            return text
                .Replace("\\", "\\\\")
                .Replace("'", "\\'")
                .Replace("\"", "\\\"");
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadTourApiDataAsync("제주");
        }

        private async Task LoadTourApiDataAsync(string keyword)
        {
            try
            {
                TxtResultSub.Text = "관광공사 API 조회 중...";

                List<TourSpotItem> list = await _tourApiService.SearchTourSpotsAsync(keyword, 20);

                _tourList = list;

                ApplyTourSort();

                TxtResultCount.Text = _tourList.Count.ToString();
                TxtResultSub.Text = "검색어 · " + keyword;

                if (_tourList.Count > 0)
                {
                    TourSpotList.SelectedItem = _tourList[0];
                    SetSelectedTour(_tourList[0]);
                }
                else
                {
                    MessageBox.Show("검색 결과가 없습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "관광공사 API 오류");
            }
        }

        private async Task LoadNearbyStayPreviewAsync(TourSpotItem tour)
        {
            if (tour == null)
                return;

            if (tour.MapX == 0 || tour.MapY == 0)
                return;

            try
            {
                StayList.ItemsSource = null;

                _nearbyStayList = await _tourApiService.GetNearbyStaysAsync(
                    mapX: tour.MapX,
                    mapY: tour.MapY,
                    radius: 20000,
                    numOfRows: 50);

                // 메인 화면에는 가까운 숙소 4개만 미리보기
                StayList.ItemsSource = _nearbyStayList
                    .Take(4)
                    .ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "주변 숙소 조회 오류");
            }
        }

        public void SetLoginUser(string userId)
        {
            _isLoggedIn = true;
            _loginUserId = userId;

            BtnLogin.Visibility = Visibility.Collapsed;

            TxtLoginUser.Text = "👤 " + _loginUserId;
            UserBox.Visibility = Visibility.Visible;

            BtnLogout.Visibility = Visibility.Visible;
        }

        private void BtnSortDistance_Click(object sender, RoutedEventArgs e)
{
            Button btn = sender as Button;

            if (btn != null)
                SetSelectedSortButton(btn);

            _sortMode = "거리순";
            ApplyTourSort();
        }

        private void BtnSortRating_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn != null)
                SetSelectedSortButton(btn);

            _sortMode = "별점순";
            ApplyTourSort();
        }

private void BtnSortReview_Click(object sender, RoutedEventArgs e)
{
            Button btn = sender as Button;

            if (btn != null)
                SetSelectedSortButton(btn);

            _sortMode = "리뷰순";
            ApplyTourSort();
        }

private void ApplyTourSort()
{
    if (_tourList == null)
        return;

    List<TourSpotItem> sorted;

    if (_sortMode == "별점순")
    {
        sorted = _tourList
            .OrderByDescending(x => GetRatingValue(x))
            .ToList();
    }
    else if (_sortMode == "리뷰순")
    {
        sorted = _tourList
            .OrderByDescending(x => GetReviewScore(x))
            .ToList();
    }
    else
    {
        // 기본은 거리순
        double baseMapX;
        double baseMapY;

        GetCurrentRegionCenter(out baseMapX, out baseMapY);

        sorted = _tourList
            .OrderBy(x => CalculateDistanceMeters(baseMapY, baseMapX, x.MapY, x.MapX))
            .ToList();
    }

    // 번호 다시 매기기
    for (int i = 0; i < sorted.Count; i++)
    {
        sorted[i].No = i + 1;
    }

    TourSpotList.ItemsSource = null;
    TourSpotList.ItemsSource = sorted;

    if (sorted.Count > 0)
    {
        TourSpotList.SelectedItem = sorted[0];
        SetSelectedTour(sorted[0]);
    }
}

        private double GetRatingValue(TourSpotItem item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.Rating))
                return 0;

            // 예: "★ 4.7" → 4.7
            string text = item.Rating.Replace("★", "").Trim();

            double value;

            if (double.TryParse(text, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return value;
            }

            return 0;
        }

        private int GetReviewScore(TourSpotItem item)
        {
            if (item == null)
                return 0;

            // 관광공사 기본 검색 API에는 리뷰 수가 없어서
            // contentId 기준으로 임시 리뷰 점수를 만들어 정렬용으로 사용
            string key = item.ContentId;

            if (string.IsNullOrWhiteSpace(key))
                key = item.Name;

            int hash = Math.Abs(key.GetHashCode());

            return 100 + (hash % 3000);
        }

        private void GetCurrentRegionCenter(out double mapX, out double mapY)
        {
            // 기본 제주 중심
            mapX = 126.5312;
            mapY = 33.3617;

            ComboBoxItem item = CmbRegion.SelectedItem as ComboBoxItem;

            if (item == null)
                return;

            string region = item.Tag.ToString();

            if (region.Contains("제주"))
            {
                mapX = 126.5312;
                mapY = 33.3617;
            }
            else if (region.Contains("대전"))
            {
                mapX = 127.3845;
                mapY = 36.3504;
            }
            else if (region.Contains("서울"))
            {
                mapX = 126.9780;
                mapY = 37.5665;
            }
            else if (region.Contains("강원"))
            {
                mapX = 127.7298;
                mapY = 37.8854;
            }
            else if (region.Contains("경상"))
            {
                mapX = 128.2132;
                mapY = 35.4606;
            }
            else if (region.Contains("전라"))
            {
                mapX = 126.9910;
                mapY = 34.8161;
            }
        }

        private int CalculateDistanceMeters(double lat1, double lng1, double lat2, double lng2)
        {
            if (lat1 == 0 || lng1 == 0 || lat2 == 0 || lng2 == 0)
                return int.MaxValue;

            const double earthRadius = 6371000;

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

        private void SetSelectedSortButton(Button selectedButton)
        {
            foreach (var child in SortPanel.Children)
            {
                Button btn = child as Button;

                if (btn != null)
                {
                    btn.Style = (Style)FindResource("GrayButton");
                }
            }

            selectedButton.Style = (Style)FindResource("YellowButton");
        }

        private void UserBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            MyPageWindow window = new MyPageWindow(_loginUserId);
            window.Owner = this;
            window.ShowDialog();
        }

        private void UserBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_loginUserId))
            {
                MessageBox.Show("로그인 정보가 없습니다.");
                return;
            }

            MyPageWindow window = new MyPageWindow(_loginUserId);
            window.Owner = this;
            window.ShowDialog();
        }
    }
}
