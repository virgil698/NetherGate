/**
 * NetherGate JavaScript/TypeScript API 类型定义
 * @version 2.0.0
 */

declare module 'nethergate' {
    // ===== 插件信息 =====

    export interface PluginInfo {
        id: string;
        name: string;
        version: string;
        description?: string;
        author?: string;
        website?: string;
    }

    // ===== 日志记录 =====

    export interface Logger {
        trace(message: string): void;
        debug(message: string): void;
        info(message: string): void;
        warning(message: string): void;
        warn(message: string): void;
        error(message: string, exception?: Error): void;
    }

    // ===== 事件系统 =====

    export interface EventBus {
        subscribe(eventName: string, handler: (event: any) => void | Promise<void>, priority?: number): void;
        unsubscribe(eventName: string, handler: Function): void;
        publish(eventName: string, eventData?: any): Promise<void>;
        clearAll(): void;
    }

    // 服务器事件
    export interface ServerStartedEvent {
        timestamp: string;
    }

    export interface ServerReadyEvent {
        timestamp: string;
        startupTimeSeconds: number;
    }

    export interface ServerStoppedEvent {
        timestamp: string;
    }

    // 玩家事件
    export interface PlayerJoinEvent {
        playerName: string;
        playerUuid: string;
        timestamp: string;
    }

    export interface PlayerLeaveEvent {
        playerName: string;
        playerUuid: string;
        timestamp: string;
    }

    // SMP 事件
    export interface SmpConnectedEvent {
        timestamp: string;
    }

    export interface SmpDisconnectedEvent {
        timestamp: string;
        reason?: string;
    }

    // 管理员事件
    export interface OperatorAddedEvent {
        operator: OperatorDto;
        timestamp: string;
    }

    export interface OperatorRemovedEvent {
        player: PlayerDto;
        timestamp: string;
    }

    // 白名单事件
    export interface AllowlistChangedEvent {
        players: PlayerDto[];
        operation: 'added' | 'removed' | 'set' | 'cleared';
        timestamp: string;
    }

    // 封禁事件
    export interface PlayerBannedEvent {
        ban: UserBanDto;
        timestamp: string;
    }

    export interface PlayerUnbannedEvent {
        player: PlayerDto;
        timestamp: string;
    }

    export interface IpBannedEvent {
        ban: IpBanDto;
        timestamp: string;
    }

    export interface IpUnbannedEvent {
        ip: string;
        timestamp: string;
    }

    // 游戏规则事件
    export interface GameRuleChangedEvent {
        rule: string;
        newValue: any;
        timestamp: string;
    }

    // ===== SMP API (Server Management Protocol) =====

    export interface PlayerDto {
        name: string;
        uuid: string;
    }

    export interface UserBanDto {
        player: PlayerDto;
        created: string;
        source: string;
        expires?: string;
        reason?: string;
    }

    export interface IpBanDto {
        ip: string;
        created: string;
        source: string;
        expires?: string;
        reason?: string;
    }

    export interface OperatorDto {
        player: PlayerDto;
        level: number;
        bypassesPlayerLimit: boolean;
    }

    export interface GameVersion {
        name: string;
        protocol: number;
    }

    export interface ServerState {
        started: boolean;
        motd?: string;
        version?: GameVersion;
        maxPlayers?: number;
        onlinePlayers?: number;
    }

    export interface TypedRule {
        type: string;
        value: any;
    }

    export interface SmpApi {
        // 白名单管理
        getAllowlist(): Promise<PlayerDto[]>;
        setAllowlist(players: PlayerDto[]): Promise<void>;
        addToAllowlist(player: PlayerDto): Promise<void>;
        removeFromAllowlist(player: PlayerDto): Promise<void>;
        clearAllowlist(): Promise<void>;

        // 封禁管理
        getBans(): Promise<UserBanDto[]>;
        setBans(bans: UserBanDto[]): Promise<void>;
        addBan(ban: UserBanDto): Promise<void>;
        removeBan(player: PlayerDto): Promise<void>;
        clearBans(): Promise<void>;

        // IP 封禁
        getIpBans(): Promise<IpBanDto[]>;
        setIpBans(bans: IpBanDto[]): Promise<void>;
        addIpBan(ban: IpBanDto): Promise<void>;
        removeIpBan(ip: string): Promise<void>;
        clearIpBans(): Promise<void>;

        // 玩家管理
        getPlayers(): Promise<PlayerDto[]>;
        kickPlayer(playerName: string, reason?: string): Promise<void>;

        // 管理员管理
        getOperators(): Promise<OperatorDto[]>;
        setOperators(operators: OperatorDto[]): Promise<void>;
        addOperator(operator: OperatorDto): Promise<void>;
        removeOperator(player: PlayerDto): Promise<void>;
        clearOperators(): Promise<void>;

        // 服务器管理
        getServerStatus(): Promise<ServerState>;
        saveWorld(): Promise<void>;
        stopServer(): Promise<void>;
        sendSystemMessage(message: string): Promise<void>;

        // 游戏规则
        getGameRules(): Promise<Record<string, TypedRule>>;
        updateGameRule(rule: string, value: any): Promise<void>;

        // 服务器设置
        getServerSettings(): Promise<Record<string, any>>;
        getServerSetting(key: string): Promise<any>;
        setServerSetting(key: string, value: any): Promise<void>;

        // 连接状态
        isConnected(): boolean;
    }

    // ===== 命令系统 =====

    export interface CommandContext {
        sender: string;
        args: string[];
        reply(message: string): Promise<void>;
    }

    export interface CommandOptions {
        name: string;
        callback: (context: CommandContext) => void | Promise<void>;
        description?: string;
        usage?: string;
        permission?: string;
        aliases?: string[];
    }

    export interface CommandRegistry {
        register(options: CommandOptions): void;
        unregister(name: string): void;
    }

    // ===== RCON 客户端 =====

    export interface RconResult {
        success: boolean;
        response: string;
    }

    export interface RconClient {
        execute(command: string): Promise<RconResult>;
        executeBatch(commands: string[]): Promise<RconResult[]>;
        isConnected(): boolean;
    }

    // ===== 日志监听器 =====

    export interface LogPattern {
        name: string;
        pattern: string | RegExp;
        handler: (match: RegExpMatchArray) => void | Promise<void>;
    }

    export interface LogListener {
        addPattern(pattern: LogPattern): void;
        removePattern(name: string): void;
        clearPatterns(): void;
        start(): Promise<void>;
        stop(): Promise<void>;
        isRunning(): boolean;
    }

    // ===== 游戏显示 API =====

    export interface GameDisplayApi {
        // Boss 血条
        showBossBar(id: string, title: string, progress: number, color?: string, style?: string): Promise<void>;
        updateBossBar(id: string, progress?: number, title?: string): Promise<void>;
        hideBossBar(id: string): Promise<void>;

        // 标题
        showTitle(selector: string, title: string, subtitle?: string, fadeIn?: number, stay?: number, fadeOut?: number): Promise<void>;
        showSubtitle(selector: string, subtitle: string): Promise<void>;
        clearTitle(selector: string): Promise<void>;

        // 动作栏
        showActionBar(selector: string, text: string): Promise<void>;

        // 聊天消息
        sendChatMessage(selector: string, message: string): Promise<void>;
        broadcastMessage(message: string): Promise<void>;

        // 告示牌编辑
        openSignEditor(playerName: string, position: Position): Promise<void>;
    }

    // ===== 游戏工具 API =====

    export interface Position {
        x: number;
        y: number;
        z: number;
    }

    export interface Region {
        from: Position;
        to: Position;
    }

    export interface FireworkOptions {
        type: 'small_ball' | 'large_ball' | 'star' | 'creeper' | 'burst';
        colors?: string[];
        fadeColors?: string[];
        flicker?: boolean;
        trail?: boolean;
        power?: number;
    }

    export interface CommandSequence {
        execute(action: () => void | Promise<void>): CommandSequence;
        waitTicks(ticks: number): CommandSequence;
        waitSeconds(seconds: number): CommandSequence;
        repeat(times: number): CommandSequence;
        run(): Promise<void>;
    }

    export interface GameUtilities {
        // 烟花系统
        launchFirework(position: Position, options: FireworkOptions): Promise<void>;
        launchFireworkShow(positions: Position[], options: FireworkOptions, intervalMs?: number): Promise<void>;

        // 命令序列
        createSequence(): CommandSequence;

        // 区域操作
        fillArea(region: Region, blockType: string): Promise<void>;
        cloneArea(source: Region, destination: Position): Promise<void>;
        setBlock(position: Position, blockType: string): Promise<void>;

        // 时间和天气
        setTime(time: number | 'day' | 'night' | 'noon' | 'midnight'): Promise<void>;
        setWeather(weather: 'clear' | 'rain' | 'thunder', duration?: number): Promise<void>;

        // 传送
        teleport(selector: string, position: Position): Promise<void>;
        teleportRelative(selector: string, offset: Position): Promise<void>;

        // 效果
        giveEffect(selector: string, effect: string, duration: number, amplifier?: number, hideParticles?: boolean): Promise<void>;
        clearEffects(selector: string): Promise<void>;

        // 粒子
        spawnParticle(particle: string, position: Position, count?: number, spread?: Position, speed?: number): Promise<void>;

        // 声音
        playSound(selector: string, sound: string, volume?: number, pitch?: number): Promise<void>;
        playSoundAt(position: Position, sound: string, volume?: number, pitch?: number): Promise<void>;
        stopSound(selector: string, sound?: string): Promise<void>;
    }

    // ===== 音乐播放器 =====

    export interface Note {
        note: string; // 'C', 'D', 'E', 'F', 'G', 'A', 'B'
        octave?: number; // 0-2
        sharp?: boolean;
    }

    export interface Melody {
        addNote(note: string | Note, durationMs: number): Melody;
        addRest(durationMs: number): Melody;
        setInstrument(instrument: string): Melody;
        setVolume(volume: number): Melody;
        play(selector: string): Promise<void>;
        loop(selector: string, times: number): Promise<void>;
    }

    export interface MusicPlayer {
        createMelody(): Melody;
        stopAll(selector: string): Promise<void>;
    }

    // ===== 调度器 =====

    export interface Scheduler {
        runDelayed(callback: () => void | Promise<void>, delayMs: number): string;
        runRepeating(callback: () => void | Promise<void>, intervalMs: number, initialDelayMs?: number): string;
        runDelayedTicks(callback: () => void | Promise<void>, ticks: number): string;
        runRepeatingTicks(callback: () => void | Promise<void>, intervalTicks: number, initialDelayTicks?: number): string;
        cancel(taskId: string): void;
        cancelAll(): void;
    }

    // ===== 配置管理 =====

    export interface ConfigManager {
        load(filename: string, defaults?: any): Promise<any>;
        get(path: string, defaultValue?: any): any;
        set(path: string, value: any): void;
        save(filename: string): Promise<void>;
        reload(filename: string): Promise<void>;
    }

    // ===== 权限管理 =====

    export interface PermissionManager {
        hasPermission(player: string, permission: string): Promise<boolean>;
        grantPermission(player: string, permission: string): Promise<void>;
        revokePermission(player: string, permission: string): Promise<void>;
        getPlayerPermissions(player: string): Promise<string[]>;
        createGroup(groupName: string): Promise<void>;
        deleteGroup(groupName: string): Promise<void>;
        addPlayerToGroup(player: string, groupName: string): Promise<void>;
        removePlayerFromGroup(player: string, groupName: string): Promise<void>;
    }

    // ===== 数据读取 =====

    export interface PlayerData {
        uuid: string;
        name?: string;
        health: number;
        maxHealth: number;
        foodLevel: number;
        xp: number;
        level: number;
        gamemode: number;
        position: Position;
        dimension: string;
        inventory: ItemStack[];
        enderItems: ItemStack[];
    }

    export interface ItemStack {
        id: string;
        count: number;
        slot?: number;
        components?: Record<string, any>;
    }

    export interface WorldData {
        name: string;
        seed: number;
        time: number;
        raining: boolean;
        thundering: boolean;
        difficulty: string;
        hardcore: boolean;
        spawnX: number;
        spawnY: number;
        spawnZ: number;
    }

    export interface PlayerDataReader {
        read(playerNameOrUuid: string): Promise<PlayerData>;
        exists(playerNameOrUuid: string): Promise<boolean>;
        getUuid(playerName: string): Promise<string | null>;
        getName(uuid: string): Promise<string | null>;
        getOnlinePlayers(): Promise<string[]>;
    }

    export interface WorldDataReader {
        read(): Promise<WorldData>;
        getWorldName(): Promise<string>;
        getSeed(): Promise<number>;
        getSpawnPoint(): Promise<Position>;
    }

    // ===== 方块数据操作 =====

    export interface ContainerData {
        position: Position;
        type: string;
        items: ItemStack[];
        customName?: string;
        lock?: string;
    }

    export interface BlockDataReader {
        getChestItems(position: Position): Promise<ItemStack[]>;
        getContainerData(position: Position): Promise<ContainerData>;
        getSignText(position: Position): Promise<string[]>;
        getBlockEntity(position: Position): Promise<any>;
    }

    export interface BlockDataWriter {
        setChestItems(position: Position, items: ItemStack[]): Promise<void>;
        setContainerItems(position: Position, items: ItemStack[]): Promise<void>;
        setSignText(position: Position, lines: string[]): Promise<void>;
        updateBlockEntity(position: Position, data: any): Promise<void>;
    }

    // ===== NBT 数据操作 =====

    export interface NbtDataWriter {
        updatePlayerHealth(playerUuid: string, health: number, maxHealth?: number): Promise<void>;
        updatePlayerFood(playerUuid: string, foodLevel: number, saturation?: number): Promise<void>;
        updatePlayerXp(playerUuid: string, xp: number, level: number): Promise<void>;
        updatePlayerPosition(playerUuid: string, position: Position, dimension?: string): Promise<void>;
        updatePlayerGamemode(playerUuid: string, gamemode: number): Promise<void>;
        setPlayerInventory(playerUuid: string, items: ItemStack[]): Promise<void>;
    }

    // ===== 物品组件系统 (1.20.5+) =====

    export interface ItemComponentReader {
        readInventorySlot(playerName: string, slot: number): Promise<ItemStack | null>;
        readHeldItem(playerName: string): Promise<ItemStack | null>;
        readEnderChestSlot(playerName: string, slot: number): Promise<ItemStack | null>;
    }

    export interface ItemComponentWriter {
        updateComponent(playerName: string, slot: number, componentKey: string, componentValue: any): Promise<void>;
        removeComponent(playerName: string, slot: number, componentKey: string): Promise<void>;
        setComponents(playerName: string, slot: number, components: Record<string, any>): Promise<void>;
    }

    // ===== 玩家档案 API =====

    export interface PlayerProfile {
        uuid: string;
        name: string;
        properties: ProfileProperty[];
    }

    export interface ProfileProperty {
        name: string;
        value: string;
        signature?: string;
    }

    export interface PlayerProfileApi {
        getProfile(playerName: string): Promise<PlayerProfile>;
        getProfileByUuid(uuid: string): Promise<PlayerProfile>;
        getSkinUrl(playerName: string): Promise<string | null>;
        getCapeUrl(playerName: string): Promise<string | null>;
    }

    // ===== 标签系统 API =====

    export interface TagApi {
        getBlockTags(block: string): Promise<string[]>;
        getItemTags(item: string): Promise<string[]>;
        getEntityTags(entity: string): Promise<string[]>;
        hasTag(type: 'block' | 'item' | 'entity', name: string, tag: string): Promise<boolean>;
        getAllTags(type: 'block' | 'item' | 'entity'): Promise<string[]>;
    }

    // ===== 计分板 =====

    export interface ScoreboardApi {
        // 目标管理
        createObjective(name: string, criterion: string, displayName: string): Promise<void>;
        removeObjective(name: string): Promise<void>;
        setDisplay(slot: string, objective: string): Promise<void>;
        modifyObjective(name: string, property: string, value: any): Promise<void>;

        // 分数管理
        addScore(objective: string, player: string, points: number): Promise<void>;
        removeScore(objective: string, player: string, points: number): Promise<void>;
        setScore(objective: string, player: string, points: number): Promise<void>;
        getScore(objective: string, player: string): Promise<number>;
        resetScore(objective: string, player: string): Promise<void>;
        getScores(objective: string): Promise<Record<string, number>>;

        // 队伍管理
        createTeam(name: string): Promise<void>;
        removeTeam(name: string): Promise<void>;
        joinTeam(team: string, members: string[]): Promise<void>;
        leaveTeam(members: string[]): Promise<void>;
        modifyTeam(name: string, option: string, value: any): Promise<void>;
    }

    // ===== 成就追踪 =====

    export interface AdvancementProgress {
        name: string;
        completed: boolean;
        progress: number;
        criteria: Record<string, boolean>;
        completedAt?: string;
    }

    export interface AdvancementTracker {
        getPlayerAdvancements(playerUuid: string): Promise<AdvancementProgress[]>;
        isAdvancementCompleted(playerUuid: string, advancement: string): Promise<boolean>;
        getCompletionPercentage(playerUuid: string): Promise<number>;
        onAdvancementCompleted(handler: (playerUuid: string, advancement: string) => void): void;
    }

    // ===== 统计追踪 =====

    export interface StatisticsData {
        playTime: number;
        deaths: number;
        mobKills: number;
        playerKills: number;
        damageDealt: number;
        damageTaken: number;
        jumps: number;
        itemsDropped: number;
        custom: Record<string, number>;
    }

    export interface StatisticsTracker {
        getPlayerStatistics(playerUuid: string): Promise<StatisticsData>;
        getStat(playerUuid: string, stat: string): Promise<number>;
        getTopPlayers(stat: string, limit: number): Promise<Array<{ uuid: string; value: number }>>;
    }

    // ===== 排行榜系统 =====

    export interface LeaderboardEntry {
        player: string;
        score: number;
        rank: number;
        metadata?: Record<string, any>;
    }

    export interface Leaderboard {
        getName(): string;
        addScore(player: string, score: number, metadata?: Record<string, any>): Promise<void>;
        getScore(player: string): Promise<number>;
        getRank(player: string): Promise<number>;
        getTop(limit: number): Promise<LeaderboardEntry[]>;
        remove(player: string): Promise<void>;
        clear(): Promise<void>;
        getAll(): Promise<LeaderboardEntry[]>;
    }

    export interface LeaderboardSystem {
        create(name: string, sortOrder?: 'ascending' | 'descending'): Promise<Leaderboard>;
        get(name: string): Promise<Leaderboard | null>;
        delete(name: string): Promise<void>;
        list(): Promise<string[]>;
    }

    // ===== 文件系统 =====

    export interface FileWatcher {
        watch(path: string, handler: (event: FileChangeEvent) => void): void;
        unwatch(path: string): void;
    }

    export interface FileChangeEvent {
        path: string;
        changeType: 'created' | 'modified' | 'deleted';
        timestamp: string;
    }

    export interface ServerFileAccess {
        readText(relativePath: string): Promise<string>;
        readBytes(relativePath: string): Promise<Uint8Array>;
        writeText(relativePath: string, content: string): Promise<void>;
        writeBytes(relativePath: string, data: Uint8Array): Promise<void>;
        exists(relativePath: string): Promise<boolean>;
        delete(relativePath: string): Promise<void>;
        listFiles(directoryPath: string): Promise<string[]>;
    }

    export interface BackupManager {
        createBackup(name?: string): Promise<string>;
        restoreBackup(backupName: string): Promise<void>;
        deleteBackup(backupName: string): Promise<void>;
        listBackups(): Promise<string[]>;
    }

    // ===== 性能监控 =====

    export interface PerformanceMetrics {
        cpuUsage: number;
        memoryUsage: number;
        memoryTotal: number;
        tps: number;
        mspt: number;
        timestamp: string;
    }

    export interface PerformanceMonitor {
        getCurrentMetrics(): Promise<PerformanceMetrics>;
        startMonitoring(intervalMs: number, callback: (metrics: PerformanceMetrics) => void): void;
        stopMonitoring(): void;
    }

    // ===== WebSocket =====

    export interface WebSocketMessage {
        type: string;
        data: any;
        timestamp: string;
    }

    export interface DataBroadcaster {
        broadcast(channel: string, data: any): Promise<void>;
        send(clientId: string, channel: string, data: any): Promise<void>;
        getConnectedClients(): string[];
        isClientConnected(clientId: string): boolean;
    }

    // ===== 插件间通信 =====

    export interface PluginMessenger {
        sendMessage(pluginId: string, channel: string, data: any): Promise<any>;
        subscribe(channel: string, handler: (data: any, sender: string) => any | Promise<any>): void;
        unsubscribe(channel: string): void;
    }

    // ===== 插件上下文 =====

    export interface PluginContext {
        readonly pluginInfo: PluginInfo;
        readonly dataDirectory: string;
        readonly logger: Logger;
        readonly eventBus: EventBus;
        readonly smpApi: SmpApi;
        readonly rconClient: RconClient | null;
        readonly commandRegistry: CommandRegistry;
        readonly scheduler: Scheduler;
        readonly configManager: ConfigManager;
        readonly permissionManager: PermissionManager;
        readonly playerDataReader: PlayerDataReader;
        readonly worldDataReader: WorldDataReader;
        readonly blockDataReader: BlockDataReader;
        readonly blockDataWriter: BlockDataWriter;
        readonly nbtDataWriter: NbtDataWriter;
        readonly itemComponentReader: ItemComponentReader;
        readonly itemComponentWriter: ItemComponentWriter;
        readonly playerProfileApi: PlayerProfileApi;
        readonly tagApi: TagApi;
        readonly scoreboardApi: ScoreboardApi;
        readonly advancementTracker: AdvancementTracker;
        readonly statisticsTracker: StatisticsTracker;
        readonly leaderboardSystem: LeaderboardSystem;
        readonly gameDisplay: GameDisplayApi;
        readonly gameUtilities: GameUtilities;
        readonly musicPlayer: MusicPlayer;
        readonly logListener: LogListener;
        readonly fileWatcher: FileWatcher;
        readonly serverFileAccess: ServerFileAccess;
        readonly backupManager: BackupManager;
        readonly performanceMonitor: PerformanceMonitor;
        readonly dataBroadcaster: DataBroadcaster;
        readonly messenger: PluginMessenger;

        getPlugin(pluginId: string): any | null;
        getAllPlugins(): any[];
    }

    // ===== 插件接口 =====

    export interface Plugin {
        info: PluginInfo;
        onLoad?(): Promise<void> | void;
        onEnable?(): Promise<void> | void;
        onDisable?(): Promise<void> | void;
        onUnload?(): Promise<void> | void;
    }

    // ===== 工具函数 =====

    export namespace Utils {
        function parsePosition(str: string): Position | null;
        function formatTime(ms: number): string;
        function sleep(ms: number): Promise<void>;
        function parsePlayerSelector(selector: string): string[];
        function formatComponent(text: string, color?: string, bold?: boolean, italic?: boolean): string;
        function uuidToString(uuid: number[]): string;
        function stringToUuid(str: string): number[];
    }
}

// ===== 全局对象 =====

/**
 * CommonJS 模块支持
 */
declare const module: {
    exports: any;
};

declare const exports: any;

declare function require(module: string): any;

