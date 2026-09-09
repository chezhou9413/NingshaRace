from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageOps


#模块职责：把绿底发型方向图整理为AI参考贴图和头部预览，不写入游戏加载目录。
MOD_ROOT = Path(__file__).resolve().parents[3]
SOURCE = Path(__file__).resolve().parents[1]
PAWNS = MOD_ROOT / "1.6/Textures/NingshaRace/Pawns/Ningsha"
OUTPUT = SOURCE / "方向贴图"
DIRECTIONS = ("south", "east", "north", "west")
NAMES = {5: "风砂碎发", 6: "流砂侧辫", 7: "扬沙高束"}

#配置职责：指定各方向包围框的宽度与左上角，按原有1024画布上的头部坐标对齐。
PLACEMENTS = {
    5: [(450, 285, 245), (440, 280, 245), (445, 290, 245)],
    6: [(430, 300, 300), (425, 300, 300), (430, 300, 300)],
    7: [(470, 240, 215), (490, 205, 215), (440, 300, 215)],
}


#函数职责：根据绿底与灰度发丝的色差提取透明度，并清除边缘溢出的绿色。
def extract_hair(image):
    pixels = []
    for red, green, blue in image.convert("RGB").get_flattened_data():
        neutral = max(red, blue)
        difference = max(0, green - neutral)
        alpha = max(0, 255 - difference)
        if difference > 110:
            alpha = 0
        elif difference < 8:
            alpha = 255
        gray = min(255, round((red + blue) * 127.5 / alpha)) if alpha else 0
        pixels.append((gray, gray, gray, alpha))
    result = Image.new("RGBA", image.size)
    result.putdata(pixels)
    bounds = result.getbbox()
    if bounds is None:
        raise ValueError("生成图没有可提取的发型像素。")
    return result.crop(bounds)


#函数职责：保持发型纵横比例，将完整发丝安置到原版凝砂头部使用的标准画布。
def place_hair(hair, placement):
    width, left, top = placement
    height = round(hair.height * width / hair.width)
    if left < 0 or top < 0 or left + width > 1024 or top + height > 1024:
        raise ValueError("发型超出目标画布。")
    result = Image.new("RGBA", (1024, 1024))
    result.alpha_composite(hair.resize((width, height), Image.Resampling.LANCZOS), (left, top))
    return result


#函数职责：拆出南、东、北三个方向，校正侧辫朝向，并提供由HAR运行时镜像的西向输入。
def build_style(index):
    sheet = Image.open(SOURCE / f"Hair{index}.sheet.png")
    images = {}
    for slot, direction in enumerate(DIRECTIONS[:3]):
        left = round(slot * sheet.width / 3)
        right = round((slot + 1) * sheet.width / 3)
        hair = extract_hair(sheet.crop((left, 0, right, sheet.height)))
        if index == 6 and direction == "east":
            hair = ImageOps.mirror(hair)
        images[direction] = place_hair(hair, PLACEMENTS[index][slot])
    #现有BodyAddon启用了flipWest，西向文件必须仍朝右，由渲染器翻转一次。
    images["west"] = images["east"].copy()
    for direction, image in images.items():
        image.save(OUTPUT / f"HairMiddle{index}_{direction}.png")
    return images


#函数职责：合成现有头部与发型方向，提供正侧背视图及透明贴图检查用的静态预览。
def build_preview(styles):
    preview = Image.new("RGB", (1600, 1080), (227, 222, 212))
    draw = ImageDraw.Draw(preview)
    font = ImageFont.truetype("C:/Windows/Fonts/msyh.ttc", 25)
    labels = {"south": "正面", "east": "右向", "north": "背面", "west": "左向"}
    for row, (index, images) in enumerate(styles.items()):
        for column, direction in enumerate(DIRECTIONS):
            head_direction = "east" if direction == "west" else direction
            head = Image.open(PAWNS / f"Head/Head_{head_direction}.png").convert("RGBA")
            head = head.resize((1024, 1024), Image.Resampling.LANCZOS)
            combined = Image.alpha_composite(head, images[direction])
            if direction == "west":
                combined = ImageOps.mirror(combined)
            combined = combined.crop((130, 170, 850, 940)).resize((292, 312), Image.Resampling.LANCZOS)
            preview.paste(combined, (column * 400 + 45, row * 360 + 35), combined)
            draw.text((column * 400 + 25, row * 360 + 8),
                      f"{index + 1}  {NAMES[index]} · {labels[direction]}", font=font, fill=(38, 35, 32))
    preview.save(SOURCE / "Preview.png")


#函数职责：生成三款发型的十二张方向贴图及统一头部预览。
def main():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    styles = {index: build_style(index) for index in NAMES}
    build_preview(styles)
    print("已生成12张1024×1024灰度透明贴图与头部预览。")


if __name__ == "__main__":
    main()
