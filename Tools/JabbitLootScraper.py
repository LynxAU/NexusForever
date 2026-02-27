#!/usr/bin/env python3
"""
JabbitHole.com Loot Table Scraper

Scrapes NPC loot tables from jabbithole.com for WildStar.
Usage: python jabbit_scraper.py [npc_id] or python jabbit_scraper.py --all
"""

import argparse
import json
import re
import sys
import time
from dataclasses import dataclass, field, asdict
from typing import Optional
from urllib.parse import urljoin

try:
    import requests
    from bs4 import BeautifulSoup
except ImportError:
    print("Missing dependencies. Install with: pip install requests beautifulsoup4")
    sys.exit(1)

BASE_URL = "https://www.jabbithole.com"
SEARCH_URL = f"{BASE_URL}/npcs"


@dataclass
class LootEntry:
    """Represents a single loot table entry."""
    item_id: int
    item_name: str
    rarity: str
    drop_rate: float
    min_quantity: int
    max_quantity: int
    loot_table_id: int


@dataclass
class NpcLoot:
    """Represents an NPC's loot table."""
    npc_id: int
    npc_name: str
    zone: str
    loot_table_id: int
    entries: list = field(default_factory=list)


class JabbitScraper:
    """Web scraper for jabbithole.com NPC data."""
    
    def __init__(self, rate_limit: float = 1.0):
        self.session = requests.Session()
        self.session.headers.update({
            'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36'
        })
        self.rate_limit = rate_limit
    
    def get_page(self, url: str) -> Optional[BeautifulSoup]:
        """Fetch a page and return parsed HTML."""
        try:
            response = self.session.get(url, timeout=30)
            response.raise_for_status()
            return BeautifulSoup(response.text, 'html.parser')
        except requests.RequestException as e:
            print(f"Error fetching {url}: {e}")
            return None
    
    def search_npc(self, name: str) -> list:
        """Search for an NPC by name."""
        # JabbitHole uses client-side search, need to use their API or scrape index
        search_url = f"{SEARCH_URL}?search={name.replace(' ', '+')}"
        soup = self.get_page(search_url)
        
        if not soup:
            return []
        
        results = []
        # Find NPC links on the page
        for link in soup.select('a[href*="/npcs/"]'):
            href = link.get('href', '')
            if '/npcs/' in href and href != '/npcs':
                npc_id = self.extract_npc_id(href)
                npc_name = link.get_text(strip=True)
                if npc_id and npc_name:
                    results.append({
                        'id': npc_id,
                        'name': npc_name,
                        'url': urljoin(BASE_URL, href)
                    })
        
        return results
    
    def extract_npc_id(self, url: str) -> Optional[int]:
        """Extract NPC ID from URL."""
        match = re.search(r'/npcs/[\w-]+-(\d+)', url)
        return int(match.group(1)) if match else None
    
    def get_npc_loot(self, npc_id: int, npc_name_slug: str = None) -> Optional[NpcLoot]:
        """Fetch loot table for a specific NPC."""
        # JabbitHole URL format: /npcs/{name-slug}-{id}
        # Example: https://www.jabbithole.com/npcs/fetid-miscreation-148
        if npc_name_slug:
            url = f"{BASE_URL}/npcs/{npc_name_slug}-{npc_id}"
        else:
            # Try common name slugs or use ID-based search
            url = f"{BASE_URL}/npcs/{npc_id}"
        soup = self.get_page(url)
        
        if not soup:
            return None
        
        # Extract NPC name
        name_elem = soup.select_one('h1.npc-name, h1.page-title, .npc-header h1')
        npc_name = name_elem.get_text(strip=True) if name_elem else f"NPC_{npc_id}"
        
        # Extract zone
        zone_elem = soup.select_one('.npc-zone, .zone, [class*="zone"]')
        zone = zone_elem.get_text(strip=True) if zone_elem else "Unknown"
        
        # Find loot table section
        loot_table_id = 0
        entries = []
        
        # Look for loot table data
        loot_section = soup.select_one('#loot, .loot-table, .npc-loot, [class*="loot"]')
        
        if loot_section:
            # Try to find loot table ID
            loot_id_elem = loot_section.select_one('[class*="lootTableId"], [data-loot-id]')
            if loot_id_elem:
                loot_id_text = loot_id_elem.get('data-loot-id') or loot_id_elem.get_text()
                match = re.search(r'\d+', loot_id_text)
                if match:
                    loot_table_id = int(match.group())
            
            # Parse loot entries
            for row in loot_section.select('tr, .loot-row, .loot-item'):
                item_link = row.select_one('a[href*="/items/"]')
                if not item_link:
                    continue
                
                item_url = item_link.get('href', '')
                item_match = re.search(r'/items/[\w-]+-(\d+)', item_url)
                if not item_match:
                    continue
                
                item_id = int(item_match.group(1))
                item_name = item_link.get_text(strip=True)
                
                # Extract rarity
                rarity = "Common"
                for cls in row.get('class', []):
                    if 'rare' in cls.lower():
                        rarity = "Rare"
                    elif 'epic' in cls.lower():
                        rarity = "Epic"
                    elif 'legendary' in cls.lower():
                        rarity = "Legendary"
                    elif 'uncommon' in cls.lower():
                        rarity = "Uncommon"
                
                # Extract drop rate
                drop_rate = 0.0
                drop_elem = row.select_one('[class*="drop"], [class*="chance"]')
                if drop_elem:
                    drop_text = drop_elem.get_text(strip=True)
                    rate_match = re.search(r'([\d.]+)%?', drop_text)
                    if rate_match:
                        drop_rate = float(rate_match.group(1))
                
                # Extract quantity
                min_qty = 1
                max_qty = 1
                qty_elem = row.select_one('[class*="quantity"], [class*="count"]')
                if qty_elem:
                    qty_text = qty_elem.get_text(strip=True)
                    qty_match = re.findall(r'\d+', qty_text)
                    if qty_match:
                        min_qty = int(qty_match[0])
                        max_qty = int(qty_match[-1]) if len(qty_match) > 1 else min_qty
                
                entries.append(LootEntry(
                    item_id=item_id,
                    item_name=item_name,
                    rarity=rarity,
                    drop_rate=drop_rate,
                    min_quantity=min_qty,
                    max_quantity=max_qty,
                    loot_table_id=loot_table_id
                ))
        
        # If no structured loot found, try alternative parsing
        if not entries:
            entries = self._parse_alternative_loot(soup, loot_table_id)
        
        return NpcLoot(
            npc_id=npc_id,
            npc_name=npc_name,
            zone=zone,
            loot_table_id=loot_table_id,
            entries=entries
        )
    
    def _parse_alternative_loot(self, soup: BeautifulSoup, loot_table_id: int) -> list:
        """Alternative loot parsing for different page layouts."""
        entries = []
        
        # Look for any table with item links
        for table in soup.select('table'):
            for row in table.select('tr'):
                item_link = row.select_one('a[href*="/items/"]')
                if not item_link:
                    continue
                
                item_url = item_link.get('href', '')
                item_match = re.search(r'/items/[\w-]+-(\d+)', item_url)
                if item_match:
                    item_id = int(item_match.group(1))
                    item_name = item_link.get_text(strip=True)
                    
                    entries.append(LootEntry(
                        item_id=item_id,
                        item_name=item_name,
                        rarity="Common",
                        drop_rate=0.0,
                        min_quantity=1,
                        max_quantity=1,
                        loot_table_id=loot_table_id
                    ))
        
        return entries
    
    def scrape_all_npcs(self, start_id: int = 1, end_id: int = 10000) -> list:
        """Attempt to scrape all NPC loot tables by ID range."""
        results = []
        
        for npc_id in range(start_id, end_id + 1):
            print(f"Checking NPC {npc_id}...", end='\r')
            
            loot = self.get_npc_loot(npc_id)
            if loot and loot.entries:
                results.append(loot)
                print(f"Found loot for NPC {npc_id}: {loot.npc_name} ({len(loot.entries)} items)")
            
            time.sleep(self.rate_limit)
        
        print()  # New line after progress
        return results
    
    def export_sql(self, npc_loots: list) -> str:
        """Generate SQL INSERT statements for loot tables."""
        sql_lines = []
        
        for loot in npc_loots:
            sql_lines.append(f"-- NPC: {loot.npc_name} (ID: {loot.npc_id}) Zone: {loot.zone}")
            sql_lines.append(f"-- Loot Table ID: {loot.loot_table_id}")
            
            for entry in loot.entries:
                sql = f"""INSERT INTO npc_loot (npc_id, loot_table_id, item_id, rarity, drop_rate, min_quantity, max_quantity)
VALUES ({loot.npc_id}, {entry.loot_table_id}, {entry.item_id}, '{entry.rarity}', {entry.drop_rate}, {entry.min_quantity}, {entry.max_quantity});"""
                sql_lines.append(sql)
            
            sql_lines.append("")
        
        return "\n".join(sql_lines)
    
    def export_json(self, npc_loots: list) -> str:
        """Export loot data as JSON."""
        return json.dumps([asdict(loot) for loot in npc_loots], indent=2)


def main():
    parser = argparse.ArgumentParser(description="Scrape NPC loot tables from JabbitHole.com")
    parser.add_argument('npc_id', nargs='?', type=int, help="NPC ID to scrape")
    parser.add_argument('--slug', '-g', help="NPC name slug (e.g., 'fetid-miscreation' for /npcs/fetid-miscreation-148)")
    parser.add_argument('--search', '-s', help="Search for NPC by name")
    parser.add_argument('--all', '-a', action='store_true', help="Scrape all NPCs (slow!)")
    parser.add_argument('--start', type=int, default=1, help="Starting NPC ID for --all")
    parser.add_argument('--end', type=int, default=1000, help="Ending NPC ID for --all")
    parser.add_argument('--output', '-o', help="Output file (default: stdout)")
    parser.add_argument('--format', choices=['json', 'sql'], default='sql', help="Output format")
    parser.add_argument('--rate', '-r', type=float, default=1.0, help="Rate limit in seconds")
    
    args = parser.parse_args()
    
    scraper = JabbitScraper(rate_limit=args.rate)
    
    if args.search:
        print(f"Searching for: {args.search}")
        results = scraper.search_npc(args.search)
        for r in results:
            print(f"  {r['id']}: {r['name']}")
        return
    
    if args.npc_id:
        print(f"Fetching loot for NPC {args.npc_id}...")
        loot = scraper.get_npc_loot(args.npc_id, args.slug)
        if loot:
            print(f"NPC: {loot.npc_name}")
            print(f"Zone: {loot.zone}")
            print(f"Loot Table ID: {loot.loot_table_id}")
            print(f"Items ({len(loot.entries)}):")
            for entry in loot.entries:
                print(f"  - {entry.item_name} ({entry.rarity}) - {entry.drop_rate}%")
            
            if args.output:
                with open(args.output, 'w') as f:
                    if args.format == 'sql':
                        f.write(scraper.export_sql([loot]))
                    else:
                        f.write(scraper.export_json([loot]))
                print(f"Exported to {args.output}")
        else:
            print("No loot data found or NPC doesn't exist")
        return
    
    if args.all:
        print(f"Scraping NPCs {args.start} to {args.end}...")
        results = scraper.scrape_all_npcs(args.start, args.end)
        print(f"Found {len(results)} NPCs with loot tables")
        
        if results:
            if args.output:
                with open(args.output, 'w') as f:
                    if args.format == 'sql':
                        f.write(scraper.export_sql(results))
                    else:
                        f.write(scraper.export_json(results))
                print(f"Exported to {args.output}")
            else:
                print(scraper.export_sql(results))
        return
    
    parser.print_help()


if __name__ == "__main__":
    main()
