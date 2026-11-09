# Level làm sạch ghế — 13 bước

Mở `Assets/MAI_ROOM BÃI BIỂN/MAI_CHAIR_CLEANING_13.unity` và nhấn Play.
Hai scene bãi biển cũng có nút **Làm sạch ghế · 13 bước**. Nút **Về biển** trở lại scene đã mở level.

## Điều khiển

- Chọn dụng cụ ở thanh dưới, giữ và kéo chuột hoặc ngón tay trên vùng được đánh dấu.
- Bước 1: dùng tay nhặt 4 con ốc biển, đổi sang kẹp mềm để gắp cua; thả vào phần cát bên trong giỏ mây ở bên phải. Thả lên thành giỏ hoặc ra ngoài sẽ trả con vật về ghế.
- Giỏ gồm lớp đáy/cát, bóng tiếp xúc, các con vật và lớp vành trước. Sinh vật có vị trí riêng, thu nhỏ và hạ vào lòng giỏ; được giới hạn trong miệng giỏ, bị vành trước che phần dưới, và tiếp tục hiện trong giỏ sau bước 1. Chơi lại sẽ trả các con về ghế và làm rỗng giỏ.
- Các bước làm sạch tính diện tích đã thao tác; giữ mãi tại một điểm không hoàn thành cả bước.
- Bước 8: chấm dung dịch lên 3 vết ố, chờ 1,1 giây, đổi sang bọt biển để lau.
- Bước 12: hút nước trên đệm, sau đó đổi sang khăn lau khung gỗ.
- Bước 13: bóp keo dọc viền rách; chọn miếng da và kéo thẻ **DA XANH NGỌC** vào vùng rách; chọn miết da và vuốt từ giữa ra mép. Đặt sai vị trí không được tính.
- Hoàn thành: ghế sạch, khô, không còn vết rách; giữ nút so sánh để xem trạng thái trước khi làm sạch. Có nút chơi lại.

## Kịch bản

Bước hút cát hiện mở ảnh `Art/Chair_Sand_Removed.png` theo đường đầu hút, thay cho lọc màu cát. Lớp bảo vệ vết rách áp dụng sau khi bỏ cát nên không giữ lại đống cát quanh miệng rách. Asset tạo bằng imagegen: “Remove every loose beige sand grain and pile from the reference chair, especially the pile above and around the tear; preserve dark mildew stains, distressed wood, chair registration and the yellow foam inside the torn hole; transparent background.”

1. Nhặt ốc và gắp cua.
2. Kẹp từng búi rong riêng và thả vào giỏ rong bên trái.
3. Quét cát trên đệm.
4. Hút cát trong khe.
5. Xịt ướt chiếc ghế.
6. Phủ bọt lên đệm.
7. Chà sạch mặt đệm.
8. Xử lý vết ố.
9. Làm sạch đường may.
10. Chà khung gỗ.
11. Rửa trôi bọt bẩn.
12. Hút nước và lau khô.
13. Bóp keo, dán da và miết kín chỗ rách.

## Thực hiện

`ChairCleaningLevel.cs` quản lý thao tác, vùng hợp lệ, thứ tự bước, cứu sinh vật và dán vá. `ChairCleaning.shader` đọc mask RGBA riêng cho sạch / bọt / nước / keo. Vết rách được giữ nguyên cho đến khi dán vá. Mask chỉ thay đổi tại nơi thao tác, rồi xử lý nốt viền nhỏ khi lượt chà đã đạt yêu cầu.

`ChairLevelEntry.cs` mở level từ hai scene bãi biển. Các vật thể khác được ẩn trong scene level để tập trung vào ghế. Cây dừa giữ pose đã duyệt và bám mép màn hình.

## Dụng cụ và hiệu ứng bề mặt

### Áp dụng từ dự án ASMR

Đã đối chiếu mã tại `D:/Unity/asmr`: `SpriteCleaningLevelController.StepDefinition.Reveal.cs` (mask mở lớp trạng thái), `ScratchCard/Shaders/MaskReveal.shader`, `AnimatedCleaningTool.Effects.cs` (particle/audio theo tiếp xúc), `WinterDoghousePresentation.Weather.cs` và `DoghouseWork.shader` (xoay UV bọt quanh điểm chà, lực tác động tăng/giảm dần).

Ghế hiện dùng hai ảnh trạng thái được vẽ riêng cùng bố cục: `Art/Chair_ASMR_Wet.png` và `Art/Chair_ASMR_Soap.png`. Mask đường xịt mở dần trạng thái tương ứng. Lớp bọt không còn lấy từ texture lặp; chà dùng phép xoay UV, influence radius 0.055–0.145 và vệt kem nổi theo cách của DoghouseWork. Dừng tay làm lực xoay giảm dần, bọt vẫn tồn tại đến bước xả.

`ASMRReference/` chứa bản sao texture Splash Effect / FX Vaccum và âm thanh nước, xà phòng, bàn chải từ `_ProjectTemplate` của ASMR. Không thay đổi file trong dự án ASMR. Sprite dụng cụ vẫn giữ art và góc nhìn theo ghế.

Hai ảnh trạng thái tạo bằng imagegen tích hợp. Prompt tóm tắt: giữ nguyên hình học, camera và bố cục ghế; trạng thái ướt có đệm xanh sẫm, nước bóng theo nếp, giọt và dòng chảy trên gỗ; trạng thái xà phòng có vệt kem/bọt theo đường may và mặt đệm, tham chiếu art level Doghouse trong ASMR, không dùng tấm texture trắng phủ phẳng.

- Góc camera của dụng cụ lấy theo chính sprite ghế: góc nhìn chéo từ trên xuống, có mặt cạnh và độ thu ngắn của vật thể theo mặt ghế. Không dùng góc chụp chính diện phẳng. Giỏ sinh vật và giỏ rong giữ góc nhìn riêng đã duyệt để nhìn rõ lòng giỏ.
- Kẹp inox đầu mềm có hai nhánh chuyển động: mở khi chờ, khép khi giữ cua hoặc rong, mở khi thả.
- Dụng cụ các bước 3–13 dùng ảnh trong `Art/CleaningTools_ChairView.png`, gồm chổi, đầu hút khe, vòi nước, bình bọt, bàn chải, dung dịch, bọt biển, khăn, keo, da và con lăn. Kẹp cua/rong dùng `Art/SoftTip_Tongs_ChairView.png`.
- Nước phun thành luồng có chuyển động; vùng đệm đã xịt sẫm màu và bóng ướt, giữ lại cho đến bước hút/lau khô.
- Bọt bám trên bề mặt theo nét kéo; bàn chải xoáy và dàn bọt, vòi xả mới rửa trôi bọt tại vị trí thao tác. Không dùng chấm trang trí để thay hiệu ứng nước/bọt.
- Điều chỉnh sau review: bọt là lớp kem mỏng theo độ sáng/nếp gấp của đệm, không phủ texture bọt macro thành một tấm dày. Bàn chải giữ lại tối thiểu 66% lượng bọt, thêm mask xoáy theo tâm nét chà; bước xả mới loại bỏ lớp bọt. Nước làm đệm thấm màu xanh đậm và khung gỗ sẫm hơn. Hút khe xử lý cả túi cát ở nếp gấp, với mức làm sạch 94% tại vùng đã hút.
- Ghế nền `Art/Chair_Dirty_Without_Kelp.png` không chứa rong. Hai búi rong là sprite riêng từ `Art/Kelp_Pickups.png`; thao tác chà không loại bỏ chúng. Phải kẹp, kéo và thả vào giỏ rong riêng, không thả chung với sinh vật.

Các asset bổ sung được tạo bằng imagegen tích hợp:

- `Art/SoftTip_Tongs_ChairView.png`: “Re-render soft-tip stainless tongs using the reference chair's elevated oblique three-quarter camera view, about 35 degrees looking down, visible thickness and foreshortened arms, angled hinge, near/far rubber jaw surfaces. Hinge centered at top, opened arms and entire tool on transparent vertical canvas. Match warm natural beach chair materials, no hands or other objects.”
- `Art/CleaningTools_ChairView.png`: “Re-render sixteen tools in the same 4×4 atlas using one camera view matching the reference lounge chair: elevated oblique front three-quarter view, near edges visible, far edges foreshortened, visible side faces, tools held just above the cushion plane. Preserve cell order, natural wood, brushed metal, muted turquoise plastic, cream cloth. Working tips upper left, no labels or background.”
- `Art/Chair_Dirty_Without_Kelp.png`: “Remove only the two hanging kelp clumps from the reference dirty chair; preserve chair composition, sand, stains, tear and weathered wood; reconstruct dirty surface beneath, transparent canvas.”
- `Art/Kelp_Pickups.png`: “Two separate dangling olive-gold marine kelp tufts matching the reference chair, left narrow/long and right fuller, wet ribbon strands, semi-realistic, transparent atlas.”
- `Art/SoapFoam_Texture.png`: “Seamless dense creamy white soap lather, irregular microbubbles and a few medium glossy bubbles, pale aqua shadows, continuous foam material, no objects or text.”

## Kiểm tra

Trong Play Mode, dùng **MAI → Chair Level → Validate 13-step playthrough** để chạy kiểm tra qua cùng các hàm tương tác mà chuột và cảm ứng sử dụng. Kiểm tra gồm dụng cụ sai, thả sai khay, dung dịch chưa ngấm, đặt da sai, yêu cầu miết da, trạng thái sạch/khô/hết rách và chơi lại. Đây là lượt kiểm tra tự động nên sẽ thay đổi tiến trình level đang chơi.

Kết quả và ảnh kiểm tra đặt trong `Review/`.

## Asset ốc và cua

- File: `Art/MarineVisitors_Theme.png`.
- Tạo bằng công cụ imagegen tích hợp; hai hình được tách bằng sprite rect theo alpha trong Unity, giữ nguyên ảnh gốc.
- Tham chiếu phong cách: sprite ghế bẩn có sẵn trong dự án.
- Prompt cuối: “Two isolated marine animals on transparent background, semi-realistic illustrated cozy beach cleaning game art matching the reference chair. Left: living marine conch sea snail, pointed elongated ivory-peach spiral shell with subtle brown stripes and natural sandy beige foot. Right: small coral-red shore crab with softened natural anatomy and normal tiny eyes. Warm beach sunlight, softly painted detailed materials, three-quarter top view. No exaggerated eyes, human smiles, toy-plastic surfaces, scenery or text. Each animal wholly inside its own half of the atlas.”

Hai bản thử quá chân thực / quá hoạt hình không được dùng trong dự án.

## Asset giỏ cứu hộ

- File: `Art/RescueBasket_Wicker.png`, tạo bằng imagegen tích hợp.
- Prompt: “One shallow oval woven wicker rescue basket for small marine snails and a little crab. Semi-realistic beach cleaning game style, warm honey rattan, three-quarter overhead view, clearly visible empty interior lined with damp pale sand, thick foreground wall and rounded rim, darker inner wall, coherent sunlight. No handle arch, lid, animals, text, scenery or floor rectangle. Genuine transparent alpha.”
- `RescueBasket.cs` và `RescueBasket.shader` dựng thứ tự trước/sau và cắt hiển thị sinh vật theo lòng giỏ; không sửa hoặc dán các con lên ảnh giỏ.

Foam brushing update: ported persistent stamp deformation from ASMR WinterDoghousePresentation.Foam.cs and DoghouseWork.shader. Displacement is stored per surface cell, retains painted lather detail, and remains after pointer release; the rinse mask removes coverage. Removed time-driven contact curls and tiled fluffy replacement. Validated full 13-step production playthrough and 2786 persistent stroke cells unchanged after release. Review: Review/Foam_ASMR_Persistent_Strokes.png.
