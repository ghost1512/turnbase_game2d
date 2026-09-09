# 2D Turn-Based Card Game

Game chiến đấu theo lượt trên lưới, kết hợp di chuyển quân, đánh thường và thẻ bài. Các thông số dưới đây được đối chiếu với dữ liệu local hiện tại; cần cập nhật tài liệu nếu thay đổi asset cân bằng.

## 1. Bắt đầu chơi

1. Mở dự án bằng Unity 6.
2. Mở scene **BattleScene_1** trong **Assets → Scenes**.
3. Bấm **Play**. Bảng bài tạm xuất hiện phía dưới Game View.
4. Dùng chuột chọn bài và mục tiêu, không cần Numpad.

Hạ toàn bộ Enemy để thắng. Không còn Player sống thì thua. Khi trận kết thúc, hệ thống khóa hành động và tự lưu tiến trình.

## 2. Hướng dẫn thao tác

| Thao tác | Cách thực hiện |
| --- | --- |
| Chọn người dùng bài | Click Player trên bàn trước khi chọn bài |
| Chọn bài | Click lá bài trên tay; lá đang chọn được tô màu |
| Dùng bài lên Enemy | Click Enemy trên bàn |
| Dùng bài lên đồng minh | Click Player cần nhận hiệu ứng; có thể chọn chính người dùng |
| Dùng bài lên bản thân | Click lá bài Self để dùng ngay lên bản thân |
| Hủy chọn | Bấm **Hủy chọn** |
| Kết thúc/bỏ lượt | Bấm **Kết thúc lượt** |
| Xem các lá còn lại | Cuộn ngang vùng bài |
| Di chuyển | Khi không chọn bài, chọn Player rồi click ô được tô sáng |
| Đánh thường tại chỗ | Khi không chọn bài, chọn Player rồi click Enemy trong tầm |

Nếu chưa chọn Player, hệ thống tự chọn một Player còn sống. Khi có nhiều quân, nên chọn quân trước để xác định người dùng bài.

Bảng chỉ hiển thị vòng, năng lượng, các lá bài và hai nút Hủy chọn / Kết thúc lượt. Lá bài chỉ hiện tên và cách dùng; thông tin chi tiết của quân xem trong Inspector, HP vẫn hiện dưới chân.

Phím tắt tùy chọn: **1–9/0** chọn lá thứ 1–10; **Enter** xác nhận bài Self; **Escape** hủy chọn; **Space** kết thúc lượt. Click tab Game trước khi dùng phím.

## 3. Quy tắc lượt chơi

| Thông số | Giá trị hiện tại |
| --- | --- |
| Năng lượng đầu lượt Player | 3 |
| Bài đầu trận | 3 lá ngẫu nhiên |
| Bổ sung định kỳ | 1 lá sau mỗi 2 vòng Player + Enemy hoàn tất |
| Giới hạn bài trên tay | 10 |
| Bộ bài khởi đầu | 11 lá, gồm 8 loại |
| Bộ bài và năng lượng | Dùng chung cho toàn phe Player |
| Đánh thường/di chuyển | Không trừ năng lượng bài; kết thúc lượt theo luồng hành động thường |

### Bài và hành động thường

- **Dùng bài:** tiêu năng lượng và lá bài, xử lý hiệu ứng rồi trở lại PlayerTurn. Có thể tiếp tục dùng bài nếu đủ năng lượng.
- **Đánh thường:** đánh một lần rồi kết thúc lượt Player.
- **Di chuyển:** đi tối đa MoveRange, sau đó được chọn mục tiêu đánh thường. Không có mục tiêu trong tầm thì tự kết thúc lượt.
- **Bỏ bước đánh sau di chuyển:** bấm Kết thúc lượt.
- Không thể dùng bài khi đang di chuyển, đang chọn mục tiêu đánh thường, EnemyTurn hoặc Resolution.
- Chọn sai mục tiêu, ngoài tầm hoặc thiếu năng lượng không tiêu bài/năng lượng.
- Một bài hợp lệ vẫn tiêu tài nguyên dù hiệu ứng không đem lại lợi ích, ví dụ Mend khi HP đã đầy.

Có thể dùng bài trước rồi di chuyển/đánh thường để kết thúc lượt. Theo luật hiện tại, không thể di chuyển xong rồi quay lại dùng bài trong cùng lượt.

### Các trạng thái

| Trạng thái | Công việc |
| --- | --- |
| StartTurn | Xóa Block cũ của phe; với Player còn hồi năng lượng, rút bài và lập Enemy Intent |
| PlayerTurn | Nhận thao tác của người chơi |
| Resolution | Xử lý hiệu ứng tuần tự, kiểm tra kết thúc trận; dùng bài xong có thể trả lại PlayerTurn |
| EnemyTurn | Enemy lần lượt thực hiện ý định đã công bố |
| EndTurn | Giữ bài chưa dùng trên tay; phe đang kết thúc lượt tick status |

Một vòng gồm lượt Player và lượt Enemy. RoundNumber tăng sau lượt Enemy. Quay lại PlayerTurn sau một lá bài **không** hồi năng lượng hoặc phát thêm bài ngoài lịch.

## 4. Bảng thông số từng loại bài

Sát thương trong bảng là giá trị cơ bản, trước Strength/Weak/Vulnerable và Block.

| Lá bài / ID | Năng lượng | Mục tiêu | Tầm | Hiệu ứng | Thời hạn | Sau khi dùng | Số bản khởi đầu |
| --- | ---: | --- | ---: | --- | --- | --- | ---: |
| **Strike** / strike | 1 | Enemy | 3 ô | Gây 6 sát thương | Tức thời | Về Draw và xào lại | 3 |
| **Guard** / guard | 1 | Bản thân | 0 ô | Nhận 5 Block | Đến đầu lượt tiếp theo của chủ sở hữu hoặc bị đánh hết | Về Draw và xào lại | 2 |
| **Mend** / mend | 1 | Đồng minh hoặc bản thân | 3 ô | Hồi 4 HP | Tức thời | Về Draw và xào lại | 1 |
| **Venom** / venom | 1 | Enemy | 3 ô | Poison 2: mất 2 HP mỗi lần tick | 3 lượt của mục tiêu | Về Draw và xào lại | 1 |
| **Weaken** / weaken | 1 | Enemy | 3 ô | Weak 1: sát thương gây ra còn 75% | 2 lượt của mục tiêu | Về Draw và xào lại | 1 |
| **Expose** / expose | 1 | Enemy | 3 ô | Vulnerable 1: sát thương nhận vào thành 150% | 2 lượt của mục tiêu | Về Draw và xào lại | 1 |
| **Focus** / focus | 1 | Bản thân | 0 ô | Rút thêm 2 lá, tuân thủ giới hạn Hand | Tức thời | Về Draw và xào lại | 1 |
| **Rage** / rage | 1 | Bản thân | 0 ô | Strength 2: cộng 2 sát thương vào mỗi đòn đánh thường hoặc hiệu ứng Damage | 2 lượt của người dùng | Về Draw và xào lại | 1 |

**Tầm bài dùng khoảng cách Manhattan:** khoảng cách = trị tuyệt đối chênh lệch X + trị tuyệt đối chênh lệch Y.

Ví dụ, từ (1,1) tới (3,2) là 3 ô, hợp lệ với Strike. Đi chéo một ô theo cả hai trục tính là 2 ô. Hiện chưa có vật cản che đường bắn; vật cản vẫn ảnh hưởng di chuyển.

### Khi nào nên dùng?

| Tình huống | Bài | Lưu ý |
| --- | --- | --- |
| Cần sát thương ngay | Strike | Tầm xa hơn đánh thường của Player |
| Enemy sắp đánh | Guard | Chỉ bảo vệ người dùng, không bảo vệ cả đội |
| Bị mất HP | Mend | Không vượt MaxHP, không hồi sinh; có thể dùng lại sau khi rút được lần nữa |
| Cần sát thương qua nhiều lượt | Venom | Nếu đủ 3 lần tick và không thêm stack, tổng mất 6 HP; bỏ qua Block |
| Giảm sát thương Enemy | Weaken | Làm tròn xuống sau khi nhân 75% |
| Tăng sát thương lên một Enemy | Expose | Dùng trước Strike hoặc đánh thường |
| Cần thêm lựa chọn trên tay | Focus | Không hồi năng lượng; phải còn bài có thể rút |
| Tăng sát thương nhiều đòn | Rage | Dùng trước bài Damage/đánh thường; không tăng Poison |

## 5. Cơ chế bộ bài

| Chồng bài | Ý nghĩa |
| --- | --- |
| Draw Pile | Các lá chờ được rút |
| Hand | Các lá đang trên tay |
| Discard Pile | Không sử dụng trong luật hiện tại |
| Exhaust Pile | Không sử dụng trong luật hiện tại; tất cả bài được tái sử dụng |

- Đầu trận, bộ bài được xào.
- Bài đã dùng quay ngay về Draw rồi xào cả chồng Draw. Lá vừa dùng có thể xuất hiện ở lần rút tiếp theo.
- Chỉ xào bài trong Draw; bài chưa dùng trên Hand được giữ qua cuối lượt.
- Hand đầy: dừng rút; các lá chưa rút vẫn ở Draw.
- Draw hết khi toàn bộ bài đang ở Hand: không rút thêm cho đến khi có bài được dùng và quay về Draw.
- Cờ Exhaust trên asset cũ không có tác dụng trong luật tái sử dụng hiện tại.
- Sau khi dùng một lá, vị trí các lá còn lại trên tay dịch chuyển.

## 6. HP, Block và Buff/Debuff

| Chỉ số/trạng thái | Cơ chế |
| --- | --- |
| HP | Về 0 thì quân chết và giải phóng ô |
| Block | Hấp thụ sát thương thường trước HP; cộng dồn được; mất ở đầu lượt tiếp theo của chủ sở hữu |
| Poison | Mất HP cuối lượt chủ sở hữu; bỏ qua Block và Vulnerable |
| Weak | Nhân sát thương gây ra với 0,75, làm tròn xuống |
| Vulnerable | Nhân sát thương thường nhận vào với 1,5, làm tròn xuống, rồi mới trừ Block |
| Strength | Cộng vào sát thương gốc trước Weak |

Thời hạn status giảm ở **cuối lượt của phe sở hữu status**, sau khi Poison gây sát thương. Buff 2 lượt lên Player trong lượt hiện tại tác dụng trong phần còn lại của lượt này và lượt Player kế tiếp.

Áp dụng lại cùng status: cộng lượng hiệu ứng và lấy thời hạn dài hơn. Với Weak/Vulnerable, nhiều stack không làm hệ số mạnh hơn; chỉ cần có status là bật hệ số tương ứng.

**Ví dụ tính sát thương:**

1. Strike 6 + Strength 2 = 8.
2. Có Weak: 8 × 0,75 = 6.
3. Mục tiêu có Vulnerable: 6 × 1,5 = 9.
4. Mục tiêu có 2 Block: mất 7 HP, Block còn 0.

Heal không hồi sinh. Strength/Weak không thay đổi lượng hồi máu, Block hoặc Poison.

## 7. Nhân vật và Enemy Intent

| Nhân vật | HP tối đa | Đánh thường | Di chuyển tối đa | Tầm đánh thường |
| --- | ---: | ---: | ---: | ---: |
| Player | 20 | 5 | 3 ô | 1 ô |
| Goblin | 12 | 3 | 2 ô | 1 ô |

| Intent | Hành động |
| --- | --- |
| Attack | Khóa Player có HP thấp nhất lúc lập kế hoạch; đánh nếu trong tầm, hoặc tìm đường di chuyển vào tầm rồi đánh tối đa một lần |
| Wait | Không hành động nếu không có mục tiêu lúc lập kế hoạch |

Enemy luôn tìm mục tiêu và di chuyển/tấn công mỗi vòng, kể cả vòng 3, 6, 9. Nếu mục tiêu cũ không hợp lệ, AI tìm Player khác; nếu không có đường đi hợp lệ thì ghi lý do và chờ lượt sau, không cộng Block.

Intent được công bố trước lượt Player. Số sát thương Intent đã tính modifier của Enemy, nhưng chưa tính Block/Vulnerable của mục tiêu. Ý định Attack không bảo đảm đánh trúng nếu mục tiêu di chuyển ra ngoài khả năng tiếp cận.

## 8. Ví dụ một lượt

Giả sử có Rage, Strike, Guard trên tay và Enemy trong tầm:

1. Dùng **Rage** lên bản thân: còn 2 năng lượng, nhận Strength 2.
2. Dùng **Strike** lên Enemy: còn 1 năng lượng; gây 8 sát thương nếu không có modifier/Block khác.
3. Dùng **Guard**: còn 0 năng lượng, nhận 5 Block.
4. Đánh thường nếu có mục tiêu trong tầm, hoặc bấm **Kết thúc lượt**.
5. Enemy hành động; Block hấp thụ sát thương thường.
6. Đến lượt Player tiếp theo: xóa Block cũ, cấp lại 3 năng lượng, giữ bài cũ; chỉ thêm 1 lá nếu vừa đủ 2 vòng.

Ví dụ chỉ tiếp tục nếu trận chưa kết thúc.

## 9. Save/Load

Game tự nạp tiến trình đầu trận và tự lưu khi kết thúc trận.

| Được lưu | Không lưu giữa trận |
| --- | --- |
| Phiên bản dữ liệu | HP/Block hiện tại |
| Số trận thắng/thua | Vị trí quân |
| Stage | Hand/Draw/Discard/Exhaust đang chạy |
| Danh sách ID deck, bao gồm số bản sao | Status, lượt và hiệu ứng đang xử lý |

File **player-progress-v1.json** nằm trong **Application.persistentDataPath** của Unity. Khi thay save đã có, hệ thống giữ bản trước trong file **.bak**.

Save hỏng, sai phiên bản hoặc chứa ID không tồn tại: báo lỗi, dùng deck mẫu tạm và không tự ghi đè file lỗi.

Stage tăng khi thắng; chưa tự sinh màn hoặc tăng độ khó theo Stage. Trận mới bắt đầu đầy HP và xây lại deck từ tiến trình.

## 10. Chỉnh thông số

| Muốn chỉnh | Vị trí trong Unity Project |
| --- | --- |
| Giá, tầm, mục tiêu, hiệu ứng (Exhaust hiện bị bỏ qua) | Assets → Resources → Cards → asset của lá bài |
| Danh sách bài/bộ bài khởi đầu | Assets → Resources → CardCatalog |
| Năng lượng mỗi lượt | CardCatalog → energyPerTurn |
| Lịch cấp bài | CardSupplySchedule: đầu trận 3 lá, mỗi 2 vòng thêm 1 |
| Giới hạn Hand | CardCatalog → handLimit |
| Chỉ số Player | Assets → Units → PlayerData |
| Chỉ số Goblin | Assets → Units → EnemyData |

Deck được nạp từ save có ưu tiên hơn starterDeck. Sửa starterDeck không thay đổi deck đã lưu.

Không đổi ID bài đã lưu nếu chưa có cơ chế chuyển đổi save. Các giá trị mẫu là điểm bắt đầu để playtest cân bằng.

## 11. Kiểm thử và giới hạn

- Có kiểm thử core cho bộ bài, năng lượng, status, hiệu ứng, mục tiêu và Save/Load.
- Chạy **Tests/run-core-tests.ps1** từ PowerShell tại thư mục dự án; cần .NET 10 SDK.
- UI hiện là bảng tạm hỗ trợ click; chưa có drag-and-drop.
- Bản UI đã build C#; cần kiểm tra thao tác/bố cục trực tiếp trong Unity Play Mode.
- Disable TurnManager hủy xử lý trận, không phải pause/resume. Dừng Play rồi Play lại để bắt đầu trận mới.

## 12. Ba loại Enemy Prefab

Các loại Enemy mới được quản lý trong **Assets → Units → Enemies**. Mỗi thư mục có một Prefab và một UnitData riêng; không sửa chung dữ liệu khi cân bằng từng loại.

| Loại | Thư mục | Prefab | UnitData | ID | Tầm đánh |
| --- | --- | --- | --- | --- | ---: |
| Cận chiến | Melee | EnemyMelee.prefab | EnemyMeleeData.asset | enemy_melee | 1 ô |
| Tầm trung | MidRange | EnemyMidRange.prefab | EnemyMidRangeData.asset | enemy_midrange | 2 ô |
| Tầm xa | LongRange | EnemyLongRange.prefab | EnemyLongRangeData.asset | enemy_longrange | 3 ô |

Cả ba hiện có **12 HP, 3 sát thương, di chuyển 2 ô**. Chỉ tầm đánh khác nhau để dễ kiểm thử. Có thể chỉnh các chỉ số độc lập trong UnitData tương ứng.

Cách dùng:
1. Dừng Play.
2. Kéo Prefab cần dùng từ thư mục tương ứng vào BattleScene_1.
3. Bấm Play. Enemy tự tìm ô spawn hợp lệ; TurnManager tự tìm EnemyAI và đưa vào lượt.
4. Có thể đặt cả ba Prefab để kiểm thử cùng lúc; mỗi bản được tìm vị trí spawn riêng nếu bản đồ đủ chỗ.

Tầm 2/3 ô là **tầm tối đa**, vẫn đánh được mục tiêu đứng sát. AI đứng đánh khi đã trong tầm; chỉ di chuyển khi chưa tiếp cận được mục tiêu. Chưa có cơ chế giữ khoảng cách tối thiểu hoặc tự lùi khi bị áp sát.

CardCatalog đã đăng ký cả ba UnitData theo ID để tra cứu qua GetCharacter(id). EnemyUnit.prefab và EnemyData cũ vẫn được giữ cho scene đang dùng chúng. Scene hiện tại chưa tự thêm ba Enemy mới.

## Luật cấp bài hiện tại
- Đầu trận nhận 3 lá ngẫu nhiên từ bộ bài đã xào; seed mặc định thay đổi mỗi trận.
- Vòng 1: 3 lá ban đầu. Vòng 2: không cấp thêm. Đầu vòng 3: thêm 1 lá. Đầu vòng 5, 7…: tiếp tục thêm 1 lá.
- Một vòng được tính khi Player và toàn bộ Enemy hoàn tất; dùng một lá không tính thành một vòng.
- Bài chưa dùng không bị bỏ cuối lượt. Bài đã dùng quay về Draw và được xào lại, kể cả Mend/Focus từng có cờ Exhaust.
- Vẫn hồi 3 năng lượng mỗi lượt; không thay đổi cơ chế tấn công/di chuyển.
- Giữ giới hạn Hand 10. Nếu Hand đầy ở lần cấp định kỳ thì bỏ lần cấp đó, không cộng dồn phần thưởng.
- Tổng số bản trong bộ được bảo toàn (mặc định 11). Các bản này quay vòng, không tạo thêm bản sao khi dùng.
- Hiệu ứng Focus vẫn rút thêm 2 lá khi dùng; lịch mỗi 2 vòng chỉ áp dụng việc cấp bài tự động.


## Log sát thương và HP dưới chân quân

Tự động áp dụng cho Player và mọi Enemy, kể cả Prefab kéo thêm vào scene; không cần gán component thủ công.

- Dưới chân mỗi quân hiển thị **HP hiện tại / HP tối đa**, đi theo quân khi di chuyển.
- HP còn từ 30% trở xuống chuyển chữ đỏ.
- HP cập nhật khi nhận sát thương, Poison hoặc hồi máu.
- Log sát thương ghi người gây sát thương, mục tiêu, HP thực tế mất, Block hấp thụ và HP còn lại.
- Ví dụ: **[Combat] Goblin Melee đã gây 3 sát thương HP cho Player. Block hấp thụ: 0. HP còn: 17/20.**
- Với bài: nguồn có tên người dùng và tên lá, ví dụ **Player [bài Strike]**.
- Poison được ghi riêng với nguồn **Poison**.
- Nếu Block chặn hết: ghi 0 sát thương HP và lượng Block đã hấp thụ. Đòn kết liễu chỉ ghi số HP thực tế mất, không tính phần sát thương dư.
- Focus vẫn rút 2 lá; vì quay về Draw trước khi xử lý, Focus cũng có thể rút lại chính nó. Mỗi lần dùng vẫn phải trả năng lượng.

## Chẩn đoán EnemyTurn
Xem CheckTest/README.md và chạy CheckTest/run-checks.ps1. Log [EnemyTurn] phân biệt tấn công, di chuyển và lý do không thể hành động. Enemy không còn Defend hoặc phòng thủ dự phòng; Guard của Player vẫn hoạt động.



## Xem thông số Unit trong Inspector
Chọn Player hoặc Enemy trong Hierarchy, xem component **Unit**. Inspector tùy chỉnh tự hoạt động, không cần gán thêm component.

- Trước Play: xem ID, tên, phe, sprite, mô tả, HP/sát thương gốc, tầm đi và tầm đánh từ UnitData.
- Trong Play: xem HP/MaxHP runtime, Block, sát thương sau modifier, còn sống/đang di chuyển, vị trí lưới/thế giới, ô đang chiếm và GridManager.
- Buff/Debuff: xem lượng và số lượt còn lại của Poison, Weak, Vulnerable, Strength.
- Enemy: xem Intent, mục tiêu, sát thương dự kiến, trạng thái AI và kết quả lượt gần nhất.
- Trận đấu: xem vòng, phe đang hành động, trạng thái lượt và kết quả trận.

Thông số runtime chỉ đọc để tránh sửa lệch nguồn dữ liệu. Chỉnh chỉ số gốc tại UnitData; tốc độ di chuyển và reference UnitData vẫn chỉnh được trên Unit. Khi Play, Inspector cập nhật liên tục. Chọn Object trong scene thay vì asset Prefab để xem trạng thái của quân đang chơi.

