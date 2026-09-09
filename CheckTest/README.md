# CheckTest — Enemy chỉ di chuyển và tấn công

Chạy từ PowerShell tại thư mục dự án:
./CheckTest/run-checks.ps1

latest-results.txt lưu kết quả kiểm thử và build gần nhất.

## Luật hiện tại
- Không còn Defend định kỳ hoặc cộng Block dự phòng của Enemy.
- Mỗi lượt: đánh mục tiêu trong tầm; nếu chưa trong tầm, tìm đường và di chuyển rồi đánh nếu có thể.
- Mục tiêu chết/bị chặn: tìm Player khác còn hợp lệ.
- Nếu không có đường hoặc không thể di chuyển, log lý do và chờ lượt sau. Không đi xuyên vật cản/quân khác.
- Block từ bài Guard của Player vẫn giữ nguyên.

## Kiểm thử tự động
9 kiểm thử chọn mục tiêu Enemy và 17 kiểm thử core (26 tổng).
Build toàn bộ script bằng reference Unity.
Các test dùng dữ liệu đường đi cung cấp sẵn, chưa chạy coroutine/pathfinding trong Unity Play Mode.

## Checklist Play Mode
| Ca | Kết quả mong đợi |
| --- | --- |
| Melee/MidRange/LongRange ở khoảng cách 1/2/3 ô | Đánh nếu nằm trong tầm tương ứng |
| Ngoài tầm, có đường | Di chuyển, đánh nếu vào tầm |
| Vòng 3, 6, 9 | Vẫn di chuyển/đánh; không tự cộng Block |
| Mục tiêu chết, còn Player khác | Đổi mục tiêu |
| Mục tiêu cũ bị chặn, có Player khác tiếp cận được | Chọn mục tiêu có thể tiếp cận |
| Enemy bị bao kín | Log không có đường và chờ; không cộng Block |
| Spawn sau PlanIntent | Lập kế hoạch khi tới lượt |
| Gọi ExecuteTurn hai lần trong cùng vòng | Chặn hành động trùng |
| Player dùng Guard | Vẫn nhận Block và hấp thụ sát thương |

Chưa đánh dấu các ca Play Mode là đã chạy.
