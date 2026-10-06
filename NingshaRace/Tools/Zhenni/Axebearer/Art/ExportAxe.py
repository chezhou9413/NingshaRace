import argparse
import copy
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


#在同一画布上导出斧头和斧刃火焰，保持 PSD 相对位置和真实握点。
def export_weapon(psd_path, mod):
    from PIL import Image
    from psd_tools import PSDImage
    psd = PSDImage.open(psd_path)
    group = psd[1][4]
    if psd.size != (3445, 1596) or group.name != '斧头' or len(group) != 2:
        raise ValueError('PSD 斧头分组结构已改变，需要重新核对图层和画布。')
    target = mod / '1.6/Textures/Zhenni/Axebearer/Weapon'
    target.mkdir(parents=True, exist_ok=True)
    manifest = {'source': str(psd_path), 'origin': [723, 1031], 'canvas': [576, 576], 'layers': []}
    for index, name in enumerate(('Axe', 'AxeFlame')):
        layer = group[index]
        canvas = Image.new('RGBA', (576, 576))
        canvas.alpha_composite(layer.topil().convert('RGBA'), (layer.left - 723, layer.top - 1031))
        path = target / ('Axebearer_' + name + '.png')
        canvas.save(path)
        manifest['layers'].append({'id': name, 'layer': layer.name, 'bbox': list(layer.bbox), 'output': str(path)})
    (target / '斧头图层坐标.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding='utf-8')


#首次建立独立外观；以后的素材重导出不覆盖美工已调整的参数。
def create_appearance(mod):
    target = mod / '1.6/Defs/Zhenni/Axebearer/AxebearerAppearance.xml'
    if target.exists():
        raise FileExistsError('持斧者外观已存在，不覆盖当前参数：' + str(target))
    document = ET.parse(mod / '1.6/Defs/Zhenni/Bladebearer/BladebearerAppearance.xml')
    definition = document.getroot()[0]
    definition.find('defName').text = 'NingshaRace_Zhenni_Axebearer_Appearance'
    definition.find('label').text = '震尼持斧者默认外观'
    parts = definition.find('parts')
    templates = {p.findtext('id'): copy.deepcopy(p) for p in parts if p.findtext('attachment') == 'Weapon'}
    for part in list(parts):
        if part.findtext('attachment') == 'Weapon':
            parts.remove(part)
    configurations = (
        ('Dagger', {'id': 'Axe', 'label': '战斧', 'texture': 'Zhenni/Axebearer/Weapon/Axebearer_Axe', 'bloomIntensity': '0.65'}),
        ('DaggerFlame', {'id': 'AxeFlame', 'label': '斧刃火焰', 'texture': 'Zhenni/Axebearer/Weapon/Axebearer_AxeFlame',
            'root': '(0.453125, 0.758681)', 'tip': '(0.725694, 0.921875)', 'rootLock': '0.2',
            'displacement': '(0.065, 0.03)', 'speed': '0.48', 'detailStrength': '0.25', 'breakup': '0.8'}),
        ('DaggerSparks', {'id': 'AxeSparks', 'label': '斧刃动态火星', 'texture': 'Zhenni/Axebearer/Weapon/Axebearer_AxeFlame',
            'particleFlame': 'AxeFlame', 'root': '(0.453125, 0.758681)', 'tip': '(0.725694, 0.921875)',
            'particleRate': '18', 'particleLifetime': '2.5', 'particleSpeed': '0.2',
            'particleSize': '0.011', 'particleSpread': '35', 'particleSway': '0.048'}))
    for template, values in configurations:
        part = templates[template]
        values['size'] = '(1.05, 1.05)'
        for key, value in values.items():
            node = part.find(key)
            if node is None:
                node = ET.SubElement(part, key)
            node.text = value
        parts.append(part)
    ET.indent(document, space='  ')
    document.write(target, encoding='utf-8', xml_declaration=True)


#图鉴和四向示意沿用游戏配置里的闲置角度、握点和分层，不代替游戏验收。
def export_icons(mod, preview):
    sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'Bladebearer/Art'))
    from ExportDirections import render_icon
    defs = mod / '1.6/Defs/Zhenni/Axebearer'
    textures = mod / '1.6/Textures'
    arguments = {'melee_path': defs / 'AxebearerMeleeAnimation.xml',
        'weapon_path': defs / 'AxebearerWeapon.xml', 'weapon_part': 'Axe'}
    icon = textures / 'UI/CodexEntries/NingshaRace/Zhenni/Axebearer/Axebearer.png'
    render_icon(textures, defs / 'AxebearerAppearance.xml', output=icon, **arguments)
    if preview:
        for facing in ('South', 'East', 'West', 'North'):
            render_icon(textures, defs / 'AxebearerAppearance.xml', facing, preview / (facing + '.png'), **arguments)


#显式指定源 PSD 和开发 Mod，原稿及拥刀者配置不写入。
def main():
    parser = argparse.ArgumentParser(description='导出震尼持斧者的斧头、火焰与四向图鉴。')
    parser.add_argument('--psd', type=Path, required=True)
    parser.add_argument('--mod', type=Path, required=True)
    parser.add_argument('--dependencies', type=Path, required=True)
    parser.add_argument('--create-appearance', action='store_true')
    parser.add_argument('--preview', type=Path)
    args = parser.parse_args()
    sys.path.insert(0, str(args.dependencies))
    export_weapon(args.psd, args.mod)
    if args.create_appearance:
        create_appearance(args.mod)
    export_icons(args.mod, args.preview)


if __name__ == '__main__':
    main()
