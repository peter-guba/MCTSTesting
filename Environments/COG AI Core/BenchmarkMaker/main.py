from typing import Set, List
import xml.etree.ElementTree as Et
import os
import random
from xml.dom import minidom
from shutil import copy2, copytree, rmtree

""""
Input:
    Set of AIs
    Environment
Output:
    BattleSet (n Battles)
    BenchmarkSet
        (AI_1, AI_2, BattleSet)
        (AI_1, AI_3, BattleSet)
        ...
"""

out_dir: str = os.path.join('out', 'Resources')
cms_resources_path = '../CMS.Benchmark/Resources'

max_rounds = 999999

directions = {(1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1)}

# bs_count = total_count * ratios[i]
# ds_count = total_count - bs_count
# bs_ratios: List[float] = [0.0, 0.25, 0.50, 0.75, 1.0]
repeats = 1
min_unit_count = 48
max_unit_count = 49
unit_count_step = 16
battle_layout_count = 5
bs_ratios: List[float] = [0.25, 0.75]
force_ratios: List[float] = [1.0]

battleship: str = 'battleship_0'
destroyer: str = 'destroyer_0'
environment_file: str = 'DefaultEnv.xml'

# iter _ (bs_count, destr_count) vs (bs_count, destr_count)
battle_str = '({:02d}+{:02d})vs({:02d}+{:02d})_[{:02d}]'
# (bs_count, destr_count) vs (bs_count, destr_count) _ BattleSet
battleset_str = '({:02d}+{:02d})vs({:02d}+{:02d})_BattleSet'
# ai_1 vs ai_2 - [(bs_count, destr_count) vs (bs_count, destr_count)]
benchmark_str = '{}_vs_{}-[({:02d}+{:02d})vs({:02d}+{:02d})]'
# (bs_count, destr_count) vs (bs_count, destr_count)
benchmarkset_str = '({:02d}+{:02d})vs({:02d}+{:02d})'


class Environment:
    def __init__(self, name):
        self.name = name
        self.impassable: Set[(int, int)] = set()
        self.radius: int = -1


class BsSettings:
    def __init__(self, environment: Environment):
        self.envi: Environment = environment
        self.bs_count: (int, int) = (0, 0)
        self.destr_count: (int, int) = (0, 0)


def parse_environment(env_file: str) -> Environment:
    environment: Environment = Environment(env_file[:-4])
    root = Et.parse(env_file).getroot()
    for node in root:
        if node.tag == 'Radius':
            environment.radius = int(node.text)
        elif node.tag == 'Sun':
            sun_pos = (int(node.attrib['Q']), int(node.attrib['R']))
            environment.impassable.add(sun_pos)
            for d in directions:
                environment.impassable.add((sun_pos[0] + d[0], sun_pos[1] + d[1]))
    return environment


def get_ai_names(ai_dir) -> List[str]:
    return list(map(lambda x: x[:-4], os.listdir(ai_dir)))


def get_rnd_pos(radius: int, origin: (int, int) = (0, 0)) -> (int, int):
    q = random.randint(-radius, radius)
    r = random.randint(max(-radius, -q - radius), min(radius, -q + radius))
    return q + origin[0], r + origin[1]


def is_valid(pos: (int, int), radius: int):
    return (radius >= pos[0] >= -radius and
            radius >= pos[1] >= -radius and
            radius >= -pos[0] - pos[1] >= -radius)


def find_empty(rad_around, start_pos, settings, placed):
    pos = get_rnd_pos(rad_around, start_pos)
    while (pos in settings.envi.impassable
           or pos in placed
           or not is_valid(pos, settings.envi.radius)):
        pos = get_rnd_pos(rad_around, start_pos)
    return pos


def print_xml(root, file):
    raw_xml_string = Et.tostring(root, 'utf-8')
    pretty_xml = minidom.parseString(raw_xml_string).toprettyxml(indent="  ")
    os.makedirs(os.path.dirname(file), exist_ok=True)
    with open(file, 'w') as out_file_open:
        out_file_open.write(pretty_xml)


def make_battle(battle_name: str, settings: BsSettings) -> None:
    # Choose a random position for the defender and the attacker
    # Place units around this position - do the same for the attacker
    battle_root = Et.Element('Battle')
    Et.SubElement(battle_root, 'Environment', {'Id': settings.envi.name})
    placed: Set(int, int) = set()
    for player in [0, 1]:
        p_ele = Et.SubElement(battle_root, 'Player', {'Index': str(player)})
        units = Et.SubElement(p_ele, 'Units')
        rad_around = 12
        start_pos = get_rnd_pos(settings.envi.radius - rad_around)
        while start_pos in settings.envi.impassable:
            start_pos = get_rnd_pos(settings.envi.radius - rad_around)
        for i in range(settings.bs_count[player]):
            pos = find_empty(rad_around, start_pos, settings, placed)
            placed.add(pos)
            Et.SubElement(units, 'Unit', {'Id': battleship, 'Q': str(pos[0]), 'R': str(pos[1])})
        for i in range(settings.destr_count[player]):
            pos = find_empty(rad_around, start_pos, settings, placed)
            placed.add(pos)
            Et.SubElement(units, 'Unit', {'Id': destroyer, 'Q': str(pos[0]), 'R': str(pos[1])})

    print_xml(battle_root, os.path.join(out_dir, 'Battles', battle_name + '.xml'))


def make_battle_set(settings: BsSettings) -> str:
    battleset_root = Et.Element('BattleSet')
    # Make n different battles
    for i in range(battle_layout_count):
        battle_name = battle_str.format(
            settings.bs_count[0],
            settings.destr_count[0],
            settings.bs_count[1],
            settings.destr_count[1],
            i
        )
        make_battle(battle_name, settings)
        Et.SubElement(battleset_root, 'Battle', {'Id': battle_name})

    bs_name = battleset_str.format(
        settings.bs_count[0],
        settings.destr_count[0],
        settings.bs_count[1],
        settings.destr_count[1]
    )
    print_xml(battleset_root, os.path.join(out_dir, 'BattleSets', bs_name + '.xml'))
    return bs_name


def make_benchmark(ai_1: str, ai_2: str, battle_set: str, settings: BsSettings) -> str:
    bench_name = benchmark_str.format(
        ai_1,
        ai_2,
        settings.bs_count[0],
        settings.destr_count[0],
        settings.bs_count[1],
        settings.destr_count[1]
    )

    bench_root = Et.Element('Benchmark')
    Et.SubElement(bench_root, 'MaxRounds').text = str(max_rounds)
    Et.SubElement(bench_root, 'IsSymmetric').text = 'true'
    Et.SubElement(bench_root, 'Repeats').text = str(repeats)

    p0 = Et.SubElement(bench_root, 'Player', {'Index': '0'})
    Et.SubElement(p0, 'AIRef', {'Id': ai_1})

    p1 = Et.SubElement(bench_root, 'Player', {'Index': '1'})
    Et.SubElement(p1, 'AIRef', {'Id': ai_2})

    Et.SubElement(bench_root, 'BattleSet', {'Id': battle_set})

    print_xml(bench_root, os.path.join(out_dir, 'Benchmarks', bench_name + '.xml'))
    return bench_name


def make_benchmark_set(ais: List[str], battle_set: str, settings: BsSettings):
    root = Et.Element('BenchmarkSet')
    for i in range(len(ais)):
        for j in range(i + 1, len(ais)):
            bench_name = make_benchmark(ais[i], ais[j], battle_set, settings)
            Et.SubElement(root, 'Benchmark', {'Id': bench_name})

    bench_set_name = benchmarkset_str.format(
        settings.bs_count[0],
        settings.destr_count[0],
        settings.bs_count[1],
        settings.destr_count[1]
    )
    print_xml(root, os.path.join(out_dir, 'BenchmarkSets', bench_set_name + '.xml'))


def copy_schema_files():
    copy2(os.path.join(cms_resources_path, 'Battles', 'Battle.xsd'), os.path.join(out_dir, 'Battles'))
    copy2(os.path.join(cms_resources_path, 'BattleSets', 'BattleSet.xsd'), os.path.join(out_dir, 'BattleSets'))
    copy2(os.path.join(cms_resources_path, 'Benchmarks', 'Benchmark.xsd'), os.path.join(out_dir, 'Benchmarks'))
    copy2(os.path.join(cms_resources_path, 'BenchmarkSets', 'BenchmarkSet.xsd'), os.path.join(out_dir, 'BenchmarkSets'))
    copy2(os.path.join(cms_resources_path, 'AIs', 'AI.xsd'), os.path.join(out_dir, 'AIs'))
    copy2(os.path.join(cms_resources_path, 'Environments', 'Environment.xsd'), os.path.join(out_dir, 'Environments'))
    os.makedirs(os.path.join(out_dir, 'SchemaTypes'), exist_ok=True)
    copy2(os.path.join(cms_resources_path, 'SchemaTypes', 'Types.xsd'), os.path.join(out_dir, 'SchemaTypes'))


def copy_data_files():
    os.makedirs(os.path.join(out_dir, 'Environments'), exist_ok=True)
    copy2(environment_file, os.path.join(out_dir, 'Environments'))
    copytree('ai', os.path.join(out_dir, 'AIs'))
    os.makedirs(os.path.join(out_dir, 'Units'), exist_ok=True)
    copy2(os.path.join(cms_resources_path, 'Units', battleship + '.xml'), os.path.join(out_dir, 'Units'))
    copy2(os.path.join(cms_resources_path, 'Units', destroyer + '.xml'), os.path.join(out_dir, 'Units'))


def run():
    if os.path.exists(out_dir):
        rmtree(out_dir)
    envi = parse_environment(environment_file)
    settings = BsSettings(envi)
    for unit_count in range(min_unit_count, max_unit_count, unit_count_step):
        for force_ratio in force_ratios:
            second_unit_count = int(unit_count * force_ratio)
            if force_ratio != 1.0 and second_unit_count == unit_count:
                continue
            for ratio in bs_ratios:
                settings.bs_count = (int(unit_count * ratio), int(second_unit_count * ratio))
                settings.destr_count = (unit_count - settings.bs_count[0],
                                        second_unit_count - settings.bs_count[1])
                battle_set = make_battle_set(settings)
                ais = get_ai_names('ai')
                make_benchmark_set(ais, battle_set, settings)
    copy_data_files()
    copy_schema_files()


if __name__ == '__main__':
    run()
