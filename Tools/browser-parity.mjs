// Read-only reference check: no browser source files are modified.
// Usage: node Tools/browser-parity.mjs /path/to/browser-neon-shooter
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
const source = path.resolve(process.argv[2] || '../browser-neon-shooter');
const { ShooterGame } = await import(pathToFileURL(path.join(source, 'src/assets/js/ShooterGame.js')));
const { canyonWall } = await import(pathToFileURL(path.join(source, 'src/assets/js/CanyonTerrain.js')));
const { POWERUPS } = await import(pathToFileURL(path.join(source, 'src/assets/js/Powerups.js')));
const fixtures = { version: 1, rules: [], canyon: [], powers: POWERUPS };
function snapshot(g) {
  return { phase: g.phase, wave:g.wave, score:g.score, kills:g.kills, lives:g.lives, missiles:g.missileCount,
    weapon:g.weapon, strength:g.weaponStrength, weaponTime:g.weaponTime, shield:g.shieldTime, speed:g.speedTime,
    orbits:g.orbitCount, orbitTime:g.orbitTime, companionMode:g.companionMode, companionTime:g.companionTime,
    scatterTime:g.scatterTime, fleet:g.fleet.length, fleetTime:g.fleetTime, assault:g.assaultFleet.length, assaultTime:g.assaultTime };
}
for (const actions of [[], ...Object.keys(POWERUPS).map(key => [key]), ['laser','laser','laser','laser'], ['machine','laser'], ['mega','machine'], ['orbit','orbit','orbit','orbit'], Array(15).fill('missile')]) {
  const game = new ShooterGame(()=>{},()=>.5);
  for(const action of actions) game.collectPowerup(action);
  fixtures.rules.push({actions, expected:snapshot(game)});
}
for(const y of [-40,-17,-6,-.5,0,1.5,6,17,40]) for(const phase of [-.9,.9,2.7,4.7]) fixtures.canyon.push({y,phase,expected:canyonWall(y,phase)});
fs.mkdirSync('Tests/Headless/Fixtures',{recursive:true});
fs.writeFileSync('Tests/Headless/Fixtures/browser-reference.json', JSON.stringify(fixtures,null,2)+'\n');
console.log(`Recorded ${fixtures.rules.length} browser rule scenarios, ${fixtures.canyon.length} canyon samples and 12 power definitions.`);
