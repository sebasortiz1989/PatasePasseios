#!/usr/bin/env bash
# Fill the local database with demo data, so screens have something to show.
#
#   Scripts/seed-demo.sh            seed the app's own database
#   Scripts/seed-demo.sh path.db    seed a specific file
#
# Run the app once first so the schema and the demo account exist.
#
# DESTRUCTIVE: deletes every tutor, dog, service and payment in the target
# database. The PetSitter account survives. Do not point this at real records.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
sql="$here/demo-data.sql"

command -v sqlite3 >/dev/null || { echo "sqlite3 not found. macOS ships it; on Debian: apt install sqlite3" >&2; exit 1; }
[ -f "$sql" ] || { echo "missing $sql" >&2; exit 1; }

if [ $# -ge 1 ]; then
    db="$1"
else
    # Matches Environment.SpecialFolder.LocalApplicationData, which .NET maps to
    # ~/.local/share on both macOS and Linux.
    db="${XDG_DATA_HOME:-$HOME/.local/share}/DapperDemo/DapperDemo.db"
fi

[ -f "$db" ] || { echo "No database at: $db"$'\n'"Run the app once to create it, or pass a path." >&2; exit 1; }
sqlite3 "$db" "SELECT 1 FROM PetSitter LIMIT 1;" >/dev/null 2>&1 || {
    echo "No PetSitter account in $db - run the app once so it seeds one." >&2; exit 1; }

echo "Target: $db"
read -r -p "Delete all records in this database and replace them with demo data? [y/N] " reply
[[ "$reply" =~ ^[Yy]$ ]] || { echo "Cancelled."; exit 0; }

# Dates are relative to today so the agenda always straddles now: past work to
# settle, unpaid work to chase, and bookings ahead.
if date -u -d "-1 days" +%F >/dev/null 2>&1; then
    offset() { date -u -d "$1 days" +"%Y-%m-%d %H:%M:%S"; }   # GNU
else
    offset() { date -u -v"$1"d +"%Y-%m-%d %H:%M:%S"; }        # BSD / macOS
fi

rendered=$(mktemp); trap 'rm -f "$rendered"' EXIT
cp "$sql" "$rendered"
for n in 9 8 7 6 5 4 3 2 1; do
    sed -i.bak "s/{{D-$n}}/$(offset "-$n")/g" "$rendered"
done
for n in 1 2 3 4 5 6 9; do
    sed -i.bak "s/{{D+$n}}/$(offset "+$n")/g" "$rendered"
done
rm -f "$rendered.bak"

grep -q '{{D' "$rendered" && { echo "Unsubstituted date token remains - aborting." >&2; exit 1; }

sqlite3 "$db" < "$rendered"

echo
echo "Seeded:"
sqlite3 "$db" "
 SELECT '  tutors    ' || COUNT(*) FROM Tutors
 UNION ALL SELECT '  dogs      ' || COUNT(*) FROM Dogs
 UNION ALL SELECT '  passeios  ' || COUNT(*) FROM WalkingService
 UNION ALL SELECT '  hotel     ' || COUNT(*) FROM PetHotelService
 UNION ALL SELECT '  day-care  ' || COUNT(*) FROM DayCareService
 UNION ALL SELECT '  sitting   ' || COUNT(*) FROM PetSittingService
 UNION ALL SELECT '  payments  ' || COUNT(*) FROM TutorPayments;"
echo
echo "Sign in with test@test.com / 8998"
